using FluentAssertions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Processing;

namespace ResponsiveProcessingStudio.Tests.Processing;

public class RequestClassifierTests
{
    private readonly RequestClassifier _classifier = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    [Fact]
    public async Task ClassifyAsync_WhenTypeIsUnknownAndMessageContainsCreditKeyword_ShouldSetCreditType()
    {
        var request = new SupportRequest 
        { 
            Message = "Хочу узнать ставку по кредиту",
            ServiceType = ServiceType.Unknown 
        };
        
        var result = await _classifier.ClassifyAsync(request, _ct);
        
        result.ServiceType.Should().Be(ServiceType.Credit);
        result.Status.Should().Be(RequestStatus.Created); 
    }

    [Fact]
    public async Task ClassifyAsync_WhenTypeIsAlreadySpecified_ShouldNotChangeIt()
    {
        var request = new SupportRequest 
        { 
            Message = "Любой текст",
            ServiceType = ServiceType.Mortgage 
        };
        
        var result = await _classifier.ClassifyAsync(request, _ct);
        
        result.ServiceType.Should().Be(ServiceType.Mortgage);
    }

    [Fact]
    public async Task ClassifyAsync_WhenNoKeywordsMatch_ShouldRemainUnknown()
    {
        var request = new SupportRequest 
        { 
            Message = "Просто случайный текст без смысла",
            ServiceType = ServiceType.Unknown 
        };
        
        var result = await _classifier.ClassifyAsync(request, _ct);

        result.ServiceType.Should().Be(ServiceType.Unknown);
    }

    [Fact]
    public async Task ClassifyAsync_WhenMessageContainsCardKeyword_ShouldSetDebitCardType()
    {
        var request = new SupportRequest 
        { 
            Message = "Не проходит оплата картой в магазине",
            ServiceType = ServiceType.Unknown 
        };

        var result = await _classifier.ClassifyAsync(request, _ct);

        result.ServiceType.Should().Be(ServiceType.DebitCard);
    }
}