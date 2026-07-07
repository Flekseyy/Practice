using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface IRequestProcessor
{
    Task<SupportRequest> ProcessAsync(SupportRequest request, int errorPercent, CancellationToken ct);
}
