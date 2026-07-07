using System.ComponentModel.DataAnnotations;
using ResponsiveProcessingStudio.Core.Pipeline;

namespace ResponsiveProcessingStudio.Web.Contracts;

public sealed class PipelineOptionsDto
{
    [Range(1, 64)]
    public int WorkersCount { get; init; } = 5;

    [Range(1, 10_000)]
    public int QueueSize { get; init; } = 100;

    [Range(0, 10)]
    public int RetriesCount { get; init; } = 3;

    [Range(0, 60_000)]
    public int RetryDelayMs { get; init; } = 500;

    [Range(0, 100)]
    public int ErrorPercent { get; init; } = 10;

    public PipelineOptions ToPipelineOptions()
    {
        return new PipelineOptions
        {
            WorkersCount = WorkersCount,
            QueueSize = QueueSize,
            RetriesCount = RetriesCount,
            RetryDelay = TimeSpan.FromMilliseconds(RetryDelayMs),
            ErrorPercent = ErrorPercent
        };
    }
}
