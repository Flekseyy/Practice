using FluentAssertions;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Processing;
using NSubstitute;

namespace ResponsiveProcessingStudio.Tests.Processing;

public class RequestValidatorTests
{
    private readonly IErrorSimulator _simulator = Substitute.For<IErrorSimulator>();
    private readonly RequestValidator _validator;
    private readonly CancellationToken _ct = CancellationToken.None;

    public RequestValidatorTests()
    {
        // По умолчанию ошибки нет, тестируем логику валидации полей
        _simulator.ShouldFail(Arg.Any<int>()).Returns(false);
        _validator = new RequestValidator(_simulator);
    }

    [Fact]
    public async Task ValidateAsync_WhenAllFieldsAreValid_ShouldReturnRequest()
    {
        var request = new SupportRequest 
        { 
            ClientName = "Иван", 
            Message = "Нужна помощь" 
        };
        
        var result = await _validator.ValidateAsync(request, 0, _ct);
        
        result.Should().BeSameAs(request);
        result.Status.Should().Be(RequestStatus.Created);
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_WhenClientNameIsEmpty_ShouldThrow()
    {
        var request = new SupportRequest 
        { 
            ClientName = "", 
            Message = "Нужна помощь" 
        };
        
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _validator.ValidateAsync(request, 0, _ct));
    }

    [Fact]
    public async Task ValidateAsync_WhenMessageIsEmpty_ShouldThrow()
    {
        var request = new SupportRequest 
        { 
            ClientName = "Иван", 
            Message = "" 
        };
        
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _validator.ValidateAsync(request, 0, _ct));
    }
}