using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Pipeline;

namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface IPipelineMetrics
{
    PipelineSnapshot GetSnapshot();
    void OnStatusChanged(RequestStatus oldStatus, RequestStatus newStatus);
    void IncrementRetry();
}