using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface IRequestAssigner
{
    Task<SupportRequest> AssignAsync(SupportRequest request, CancellationToken ct);
}