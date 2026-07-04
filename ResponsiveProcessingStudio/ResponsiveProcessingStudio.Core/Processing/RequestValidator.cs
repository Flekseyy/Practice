using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Processing;

public class RequestValidator : IRequestValidator
{
    private readonly IErrorSimulator _errorSimulator;
    private readonly int _errorPercent;

    public RequestValidator(IErrorSimulator errorSimulator, int errorPercent = 20)
    {
        _errorSimulator = errorSimulator;
        _errorPercent = errorPercent;
    }

    public async Task<SupportRequest> ValidateAsync(SupportRequest request, CancellationToken ct)
    {
        request.Status = RequestStatus.Validating;
        request.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(request.ClientName))
        {
            throw new InvalidOperationException("Имя клиента не указано");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Текст обращения не указан");
        }

        if (request.ServiceType == ServiceType.Unknown)
        {
            throw new InvalidOperationException("Тип услуги не определён после классификации");
        }

        await Task.Delay(300, ct);

        if (_errorSimulator.ShouldFail(_errorPercent))
        {
            throw new InvalidOperationException("Сбой при проверке заявки (симуляция)");
        }

        return request;
    }
}