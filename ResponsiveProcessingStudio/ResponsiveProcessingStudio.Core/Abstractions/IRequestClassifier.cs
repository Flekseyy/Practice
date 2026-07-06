using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface IRequestClassifier
{
    Task<SupportRequest> ClassifyAsync(SupportRequest request, CancellationToken ct);
}