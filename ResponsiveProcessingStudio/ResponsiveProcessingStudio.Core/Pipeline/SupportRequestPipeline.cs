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
    private readonly IRetryPolicy _retryPolicy;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    private CancellationTokenSource? _runCts;
    private CancellationTokenSource? _retryDelayCts;
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
        IPipelineMetrics metrics,
        IRetryPolicy retryPolicy)
    {
        _classifier = classifier;
        _validator = validator;
        _assigner = assigner;
        _processor = processor;
        _stateStore = stateStore;
        _metrics = metrics;
        _retryPolicy = retryPolicy;
    }

    public async Task StartAsync(PipelineOptions options, CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            if (_status != PipelineStatus.Stopped)
            {
                await StopCoreAsync();
            }

            _options = NormalizeOptions(options);
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _retryDelayCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var transformOptions = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = _options.QueueSize,
                MaxDegreeOfParallelism = _options.WorkersCount,
                EnsureOrdered = false
            };

            _inputBuffer = new BufferBlock<SupportRequest>(new DataflowBlockOptions
            {
                BoundedCapacity = _options.QueueSize
            });

            _classifierBlock = new TransformBlock<SupportRequest, SupportRequest>(
                req => RunStageAsync(req, RequestStatus.Classifying, _classifier.ClassifyAsync),
                transformOptions);

            _validatorBlock = new TransformBlock<SupportRequest, SupportRequest>(
                req => RunStageWithRetryAsync(
                    req,
                    RequestStatus.Validating,
                    (request, stageCt) => _validator.ValidateAsync(request, _options.ErrorPercent, stageCt)),
                transformOptions);

            _assignerBlock = new TransformBlock<SupportRequest, SupportRequest>(
                req => RunStageAsync(req, RequestStatus.Assigning, _assigner.AssignAsync),
                transformOptions);

            _processorBlock = new TransformBlock<SupportRequest, SupportRequest>(
                req => RunStageWithRetryAsync(
                    req,
                    RequestStatus.Processing,
                    (request, stageCt) => _processor.ProcessAsync(request, _options.ErrorPercent, stageCt)),
                transformOptions);

            _finalizeBlock = new ActionBlock<SupportRequest>(
                request =>
                {
                    _stateStore.Update(request);
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = _options.QueueSize,
                    MaxDegreeOfParallelism = _options.WorkersCount,
                    EnsureOrdered = false
                });

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            _inputBuffer.LinkTo(_classifierBlock, linkOptions);
            _classifierBlock.LinkTo(_validatorBlock, linkOptions);
            _validatorBlock.LinkTo(_assignerBlock, linkOptions);
            _assignerBlock.LinkTo(_processorBlock, linkOptions);
            _processorBlock.LinkTo(_finalizeBlock, linkOptions);

            _status = PipelineStatus.Running;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task<SupportRequest> RunStageAsync(
        SupportRequest request,
        RequestStatus stageStatus,
        Func<SupportRequest, CancellationToken, Task<SupportRequest>> action)
    {
        if (IsTerminal(request.Status))
        {
            return request;
        }

        MoveToStatus(request, stageStatus);

        try
        {
            var result = await action(request, _runCts?.Token ?? CancellationToken.None);
            _stateStore.Update(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            MarkTerminal(request, RequestStatus.Cancelled);
            return request;
        }
        catch (Exception ex)
        {
            MarkFailed(request, ex);
            return request;
        }
    }

    private async Task<SupportRequest> RunStageWithRetryAsync(
        SupportRequest request,
        RequestStatus stageStatus,
        Func<SupportRequest, CancellationToken, Task<SupportRequest>> action)
    {
        if (IsTerminal(request.Status))
        {
            return request;
        }

        MoveToStatus(request, stageStatus);

        var result = await _retryPolicy.ExecuteAsync(
            stageCt => action(request, stageCt),
            request,
            _options.RetriesCount,
            _options.RetryDelay,
            _runCts?.Token ?? CancellationToken.None,
            _retryDelayCts?.Token ?? CancellationToken.None);

        if (result.Status != stageStatus)
        {
            _metrics.OnStatusChanged(stageStatus, result.Status);
        }

        if (IsTerminal(result.Status))
        {
            _stateStore.Update(result);
            return result;
        }

        _stateStore.Update(result);
        return result;
    }

    private void MoveToStatus(SupportRequest request, RequestStatus newStatus)
    {
        var oldStatus = request.Status;
        request.Status = newStatus;
        request.UpdatedAt = DateTime.UtcNow;
        request.LastError = null;
        _stateStore.Update(request);
        _metrics.OnStatusChanged(oldStatus, newStatus);
    }

    private void MarkTerminal(SupportRequest request, RequestStatus status)
    {
        var oldStatus = request.Status;
        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        _stateStore.Update(request);
        _metrics.OnStatusChanged(oldStatus, status);
    }

    private void MarkFailed(SupportRequest request, Exception ex)
    {
        var oldStatus = request.Status;
        request.Status = RequestStatus.Failed;
        request.LastError = ex.Message;
        request.UpdatedAt = DateTime.UtcNow;
        _stateStore.Update(request);
        _metrics.OnStatusChanged(oldStatus, RequestStatus.Failed);
    }

    private static bool IsTerminal(RequestStatus status)
    {
        return status is RequestStatus.Completed or RequestStatus.Failed or RequestStatus.Cancelled;
    }

    private static PipelineOptions NormalizeOptions(PipelineOptions options)
    {
        return new PipelineOptions
        {
            WorkersCount = Math.Clamp(options.WorkersCount, 1, 64),
            QueueSize = Math.Clamp(options.QueueSize, 1, 10_000),
            RetriesCount = Math.Clamp(options.RetriesCount, 0, 10),
            RetryDelay = options.RetryDelay < TimeSpan.Zero
                ? TimeSpan.Zero
                : options.RetryDelay,
            ErrorPercent = Math.Clamp(options.ErrorPercent, 0, 100)
        };
    }

    public async Task<bool> SendAsync(SupportRequest request, CancellationToken ct)
    {
        if (_inputBuffer == null || _status != PipelineStatus.Running)
        {
            return false;
        }

        request.Status = RequestStatus.Waiting;
        request.UpdatedAt = DateTime.UtcNow;
        _stateStore.Add(request);
        _metrics.OnStatusChanged(RequestStatus.Created, RequestStatus.Waiting);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var accepted = await _inputBuffer.SendAsync(request, timeoutCts.Token);
            if (accepted)
            {
                return true;
            }

            MarkFailed(request, new InvalidOperationException("Pipeline stopped accepting requests"));
            return false;
        }
        catch (OperationCanceledException)
        {
            MarkFailed(request, new InvalidOperationException("Очередь заявок переполнена"));
            return false;
        }
    }

    public async Task StopAsync()
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            await StopCoreAsync();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task StopCoreAsync()
    {
        if (_inputBuffer == null || _status == PipelineStatus.Stopped)
        {
            return;
        }

        _status = PipelineStatus.Stopping;
        _retryDelayCts?.Cancel();

        try
        {
            _inputBuffer.Complete();
        }
        catch
        {
            // Dataflow Complete is best-effort during shutdown.
        }

        if (_finalizeBlock != null)
        {
            try
            {
                await _finalizeBlock.Completion;
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception)
            {
            }
        }

        _retryDelayCts?.Dispose();
        _retryDelayCts = null;
        _runCts?.Dispose();
        _runCts = null;

        _inputBuffer = null;
        _classifierBlock = null;
        _validatorBlock = null;
        _assignerBlock = null;
        _processorBlock = null;
        _finalizeBlock = null;

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
        _retryDelayCts?.Cancel();
        _runCts?.Cancel();
        _retryDelayCts?.Dispose();
        _runCts?.Dispose();
        _lifecycleGate.Dispose();
    }
}
