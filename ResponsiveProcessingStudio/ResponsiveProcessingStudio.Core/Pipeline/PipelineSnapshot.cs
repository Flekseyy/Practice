using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Pipeline;

public sealed class PipelineSnapshot
{
    public int WaitingCount { get; init; }
    public int ClassifyingCount { get; init; }
    public int ValidatingCount { get; init; }
    public int AssigningCount { get; init; }
    public int ProcessingCount { get; init; }
    public int CompletedCount { get; init; }
    public int FailedCount { get; init; }
    public int CancelledCount { get; init; }
    public int RetryCount { get; init; }
    public int WorkersCount { get; init; }
    public string PipelineStatus { get; init; } = "Stopped";
    public IReadOnlyCollection<SupportRequest> RecentRequests { get; init; } = Array.Empty<SupportRequest>();
}
