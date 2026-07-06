using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface IRequestProcessor
{
    Task<SupportRequest> ProcessAsync(SupportRequest request, CancellationToken ct);
}