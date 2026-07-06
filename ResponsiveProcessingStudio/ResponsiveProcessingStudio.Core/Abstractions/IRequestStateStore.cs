using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Pipeline;

namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface IRequestStateStore
{
    void Add(SupportRequest request);
    void Update(SupportRequest request);
    void UpdateStatus(Guid requestId, RequestStatus status);
    IReadOnlyCollection<SupportRequest> GetAll();
    IReadOnlyCollection<SupportRequest> GetRecent(int count);
}