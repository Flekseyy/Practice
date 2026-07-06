using System.Collections.Concurrent;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

public class RequestStateStore : IRequestStateStore
{
    private readonly ConcurrentDictionary<Guid, SupportRequest> _requests = new();

    public void Add(SupportRequest request)
    {
        _requests[request.Id] = request;
    }

    public void Update(SupportRequest request)
    {
        request.UpdatedAt = DateTime.UtcNow;
        _requests[request.Id] = request;
    }

    public void UpdateStatus(Guid id, RequestStatus status)
    {
        if (_requests.TryGetValue(id, out var request))
        {
            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;
        }
    }

    public IReadOnlyCollection<SupportRequest> GetAll()
    {
        return _requests.Values.ToList();
    }

    public IReadOnlyCollection<SupportRequest> GetRecent(int count)
    {
        return _requests.Values
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToList();
    }
}