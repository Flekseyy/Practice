using System.Threading.Tasks.Dataflow;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Pipeline;

public sealed class SupportRequestPipeline : ISupportRequestPipeline, IDisposable
{
    private readonly IRequestClassifier _classifier;
    private readonly IRequestValidator _validator;
    private readonly IRequestAssigner _assigner;
    private readonly IRequestProcessor _processor;
    private readonly IRequestStateStore _stateStore;
    private readonly IPipelineMetrics _metrics;

    private CancellationTokenSource? _stopRequestedCts;
    private PipelineOptions _options = new();
    private volatile PipelineStatus _status = PipelineStatus.Stopped;

    private BufferBlock<SupportRequest>? _inputBuffer;
    private TransformBlock<SupportRequest, SupportRequest>? _classifierBlock;
    private TransformBlock<SupportRequest, SupportRequest>? _validatorBlock;
    private TransformBlock<SupportRequest, SupportRequest>? _assignerBlock;
    private TransformBlock<SupportRequest, SupportRequest>? _processorBlock;
    private ActionBlock<SupportRequest>? _finalizeBlock;

    public SupportRequestPipeline(
        IRequestClassifier classifier,
        IRequestValidator validator,
        IRequestAssigner assigner,
        IRequestProcessor processor,
        IRequestStateStore stateStore,
        IPipelineMetrics metrics)
    {
        _classifier = classifier;
        _validator = validator;
        _assigner = assigner;
        _processor = processor;
        _stateStore = stateStore;
        _metrics = metrics;
    }

    public async Task StartAsync(PipelineOptions options, CancellationToken ct = default)
    {
        if (_status != PipelineStatus.Stopped)
            await StopAsync();

        _options = options;
        _stopRequestedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var processToken = _stopRequestedCts.Token;

        var blockOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = options.QueueSize,
            MaxDegreeOfParallelism = options.WorkersCount,
            EnsureOrdered = false
        };

        _inputBuffer = new BufferBlock<SupportRequest>(new DataflowBlockOptions
        {
            BoundedCapacity = options.QueueSize
        });

        _classifierBlock = new TransformBlock<SupportRequest, SupportRequest>(
            req => SafeProcessAsync(req, _classifier.ClassifyAsync, processToken, RequestStatus.Classifying),
            blockOptions);

        _validatorBlock = new TransformBlock<SupportRequest, SupportRequest>(
            req => SafeProcessAsync(req, _validator.ValidateAsync, processToken, RequestStatus.Validating),
            blockOptions);

        _assignerBlock = new TransformBlock<SupportRequest, SupportRequest>(
            req => SafeProcessAsync(req, _assigner.AssignAsync, processToken, RequestStatus.Assigning),
            blockOptions);

        _processorBlock = new TransformBlock<SupportRequest, SupportRequest>(
            req => SafeProcessAsync(req, _processor.ProcessAsync, processToken, RequestStatus.Processing),
            blockOptions);

        _finalizeBlock = new ActionBlock<SupportRequest>(
            request =>
            {
                if (request.Status == RequestStatus.Completed)
                {
                    _metrics.OnStatusChanged(RequestStatus.Processing, RequestStatus.Completed);
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
            });

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        _inputBuffer.LinkTo(_classifierBlock, linkOptions);
        _classifierBlock.LinkTo(_validatorBlock, linkOptions);
        _validatorBlock.LinkTo(_assignerBlock, linkOptions);
        _assignerBlock.LinkTo(_processorBlock, linkOptions);
        _processorBlock.LinkTo(_finalizeBlock, linkOptions);

        _status = PipelineStatus.Running;
    }

    private async Task<SupportRequest> SafeProcessAsync(
        SupportRequest req,
        Func<SupportRequest, CancellationToken, Task<SupportRequest>> action,
        CancellationToken ct,
        RequestStatus nextStatus)
    {
        if (req.Status == RequestStatus.Cancelled || req.Status == RequestStatus.Failed)
            return req;

        var oldStatus = req.Status;
        try
        {
            req.Status = nextStatus;
            _stateStore.UpdateStatus(req.Id, nextStatus);
            _metrics.OnStatusChanged(oldStatus, nextStatus);

            var result = await action(req, ct);
            _stateStore.Update(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            req.Status = RequestStatus.Cancelled;
            _stateStore.UpdateStatus(req.Id, RequestStatus.Cancelled);
            _metrics.OnStatusChanged(nextStatus, RequestStatus.Cancelled);
            return req;
        }
        catch (Exception ex)
        {
            req.Status = RequestStatus.Failed;
            req.LastError = ex.Message;
            _stateStore.Update(req);
            _metrics.OnStatusChanged(nextStatus, RequestStatus.Failed);
            return req;
        }
    }

    public async Task<bool> SendAsync(SupportRequest request, CancellationToken ct)
    {
        if (_inputBuffer == null || _status != PipelineStatus.Running)
            return false;

        _stateStore.Add(request);
        _metrics.OnStatusChanged(RequestStatus.Created, RequestStatus.Waiting);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            return await _inputBuffer.SendAsync(request, timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            request.Status = RequestStatus.Failed;
            request.LastError = "Очередь заявок переполнена";
            _stateStore.Update(request);
            return false;
        }
    }

    public async Task StopAsync()
    {
        if (_inputBuffer == null) return;

        _status = PipelineStatus.Stopping;
        _stopRequestedCts?.Cancel();

        try { _inputBuffer.Complete(); } catch { /* ignored */ }
        
        if (_finalizeBlock != null)
        {
            try
            {
                await _finalizeBlock.Completion;
            }
            catch (TaskCanceledException) { /* ignored */ }
            catch (Exception) { /* ignored */ }
        }

        _stopRequestedCts?.Dispose();
        _stopRequestedCts = null;
        _status = PipelineStatus.Stopped;
    }

    public PipelineSnapshot GetSnapshot()
    {
        var all = _stateStore.GetAll();

        return new PipelineSnapshot
        {
            WaitingCount = all.Count(r => r.Status == RequestStatus.Waiting),
            ClassifyingCount = all.Count(r => r.Status == RequestStatus.Classifying),
            ValidatingCount = all.Count(r => r.Status == RequestStatus.Validating),
            AssigningCount = all.Count(r => r.Status == RequestStatus.Assigning),
            ProcessingCount = all.Count(r => r.Status == RequestStatus.Processing),
            CompletedCount = all.Count(r => r.Status == RequestStatus.Completed),
            FailedCount = all.Count(r => r.Status == RequestStatus.Failed),
            CancelledCount = all.Count(r => r.Status == RequestStatus.Cancelled),
            RetryCount = all.Sum(r => r.RetryCount),
            WorkersCount = _options.WorkersCount,
            PipelineStatus = _status.ToString(),
            RecentRequests = _stateStore.GetRecent(20)
        };
    }

    public void Dispose()
    {
        _stopRequestedCts?.Cancel();
        _stopRequestedCts?.Dispose();
    }
}