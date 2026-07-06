using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface IRequestValidator
{
    Task<SupportRequest> ValidateAsync(SupportRequest request, CancellationToken ct);
}