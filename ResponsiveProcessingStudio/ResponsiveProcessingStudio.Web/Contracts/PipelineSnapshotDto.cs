using ResponsiveProcessingStudio.Core.Pipeline;

namespace ResponsiveProcessingStudio.Web.Contracts;

public sealed class PipelineSnapshotDto
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
    public IReadOnlyCollection<SupportRequestResponseDto> RecentRequests { get; init; } = [];

    public int TotalCount =>
        WaitingCount + ClassifyingCount + ValidatingCount + AssigningCount +
        ProcessingCount + CompletedCount + FailedCount + CancelledCount;

    public static PipelineSnapshotDto FromDomain(PipelineSnapshot snapshot)
    {
        return new PipelineSnapshotDto
        {
            WaitingCount = snapshot.WaitingCount,
            ClassifyingCount = snapshot.ClassifyingCount,
            ValidatingCount = snapshot.ValidatingCount,
            AssigningCount = snapshot.AssigningCount,
            ProcessingCount = snapshot.ProcessingCount,
            CompletedCount = snapshot.CompletedCount,
            FailedCount = snapshot.FailedCount,
            CancelledCount = snapshot.CancelledCount,
            RetryCount = snapshot.RetryCount,
            WorkersCount = snapshot.WorkersCount,
            PipelineStatus = snapshot.PipelineStatus,
            RecentRequests = snapshot.RecentRequests
                .Select(SupportRequestResponseDto.FromDomain)
                .ToArray()
        };
    }
}
