using FluentAssertions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.State;

namespace ResponsiveProcessingStudio.Tests.State;

public class RequestStateStoreTests
{
    private readonly RequestStateStore _store = new();

    [Fact]
    public void Add_And_GetAll_ShouldReturnAddedRequest()
    {
        var request = new SupportRequest { ClientName = "Иван" };
        
        _store.Add(request);
        var all = _store.GetAll();
        
        all.Should().ContainSingle();
        all.First().ClientName.Should().Be("Иван");
    }

    [Fact]
    public void UpdateStatus_ShouldChangeStatusInStore()
    {
        var request = new SupportRequest { Status = RequestStatus.Created };
        _store.Add(request);

        _store.UpdateStatus(request.Id, RequestStatus.Completed);
        var updated = _store.GetAll().First();

        updated.Status.Should().Be(RequestStatus.Completed);
    }

    [Fact]
    public void GetRecent_ShouldReturnSortedByCreatedAt()
    {
        var oldReq = new SupportRequest { CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var newReq = new SupportRequest { CreatedAt = DateTime.UtcNow };
        _store.Add(oldReq);
        _store.Add(newReq);

        var recent = _store.GetRecent(1);

        recent.Should().ContainSingle();
        recent.First().Id.Should().Be(newReq.Id);
    }
}