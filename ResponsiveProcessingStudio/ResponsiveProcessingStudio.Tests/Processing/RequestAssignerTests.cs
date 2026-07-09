using FluentAssertions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Factories;
using ResponsiveProcessingStudio.Core.Processing;

namespace ResponsiveProcessingStudio.Tests.Processing;

public class RequestAssignerTests
{
    private readonly RequestAssigner _assigner = new(new BankServiceFactory());
    private readonly CancellationToken _ct = CancellationToken.None;

    [Fact]
    public async Task AssignAsync_WhenTypeIsCredit_ShouldSetCreditDepartment()
    {
        var request = new SupportRequest { ServiceType = ServiceType.Credit };
        var result = await _assigner.AssignAsync(request, _ct);
        
        result.AssignedDepartment.Should().Be("Кредитный отдел");
        result.AssignedHandler.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignAsync_WhenTypeIsUnknown_ShouldSetGeneralDepartment()
    {
        var request = new SupportRequest { ServiceType = ServiceType.Unknown };
        var result = await _assigner.AssignAsync(request, _ct);
        
        result.AssignedDepartment.Should().Be("Общий отдел");
        result.AssignedHandler.Should().Be("Дежурный специалист");
    }

    [Fact]
    public async Task AssignAsync_WhenTypeIsDebitCard_ShouldSetCardDepartment()
    {
        var request = new SupportRequest { ServiceType = ServiceType.DebitCard };
        var result = await _assigner.AssignAsync(request, _ct);
        
        result.AssignedDepartment.Should().Be("Карточный отдел");
        result.AssignedHandler.Should().NotBeNull();
    }
}