using System.Collections.Concurrent;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Pipeline;

namespace ResponsiveProcessingStudio.Core.State;

public sealed class PipelineMetrics : IPipelineMetrics
{
    private readonly ConcurrentDictionary<RequestStatus, int> _statuses = new();
    private int _retryCount;

    public void OnStatusChanged(RequestStatus oldStatus, RequestStatus newStatus)
    {
        if (oldStatus != newStatus)
        {
            _statuses.AddOrUpdate(oldStatus, 0, (_, value) => Math.Max(0, value - 1));
        }

        _statuses.AddOrUpdate(newStatus, 1, (_, value) => value + 1);
    }

    public void IncrementRetry()
    {
        Interlocked.Increment(ref _retryCount);
    }

    public PipelineSnapshot GetSnapshot()
    {
        return new PipelineSnapshot
        {
            WaitingCount = GetCount(RequestStatus.Waiting),
            ClassifyingCount = GetCount(RequestStatus.Classifying),
            ValidatingCount = GetCount(RequestStatus.Validating),
            AssigningCount = GetCount(RequestStatus.Assigning),
            ProcessingCount = GetCount(RequestStatus.Processing),
            CompletedCount = GetCount(RequestStatus.Completed),
            FailedCount = GetCount(RequestStatus.Failed),
            CancelledCount = GetCount(RequestStatus.Cancelled),
            RetryCount = Volatile.Read(ref _retryCount)
        };
    }

    private int GetCount(RequestStatus status)
    {
        return _statuses.TryGetValue(status, out var count)
            ? count
            : 0;
    }
}