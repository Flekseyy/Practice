using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Processing;

public class RequestValidator : IRequestValidator
{
    private readonly IErrorSimulator _errorSimulator;

    public RequestValidator(IErrorSimulator errorSimulator)
    {
        _errorSimulator = errorSimulator;
    }

    public async Task<SupportRequest> ValidateAsync(SupportRequest request, int errorPercent, CancellationToken ct)
    {
        request.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(request.ClientName))
        {
            throw new InvalidOperationException("Имя клиента не указано");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Текст обращения не указан");
        }

        await Task.Delay(300, ct);

        if (_errorSimulator.ShouldFail(errorPercent))
        {
            throw new InvalidOperationException("Сбой при проверке заявки (симуляция)");
        }

        return request;
    }
}
