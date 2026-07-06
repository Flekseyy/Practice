namespace ResponsiveProcessingStudio.Core.Pipeline;

public sealed class PipelineOptions
{
    public int WorkersCount { get; init; } = 5;
    
    public int QueueSize { get; init; } = 20;
    
    public int RetriesCount { get; init; } = 3;
    
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(500);
    
    public int ErrorPercent { get; init; } = 10;
}