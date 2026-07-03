using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Pipeline;

namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface ISupportRequestPipeline
{
    Task StartAsync(PipelineOptions options, CancellationToken appLifetimeCt);
    Task<bool> SendAsync(SupportRequest request, CancellationToken ct);
    Task StopAsync();
    PipelineSnapshot GetSnapshot();
}