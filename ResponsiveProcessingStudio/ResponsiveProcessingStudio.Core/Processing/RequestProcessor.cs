using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Processing;

public class RequestProcessor : IRequestProcessor
{
    private readonly IErrorSimulator _errorSimulator;
    private readonly int _processingDelayMs;

    public RequestProcessor(IErrorSimulator errorSimulator, int processingDelayMs = 1000)
    {
        _errorSimulator = errorSimulator;
        _processingDelayMs = processingDelayMs;
    }

    public async Task<SupportRequest> ProcessAsync(SupportRequest request, int errorPercent, CancellationToken ct)
    {
        request.UpdatedAt = DateTime.UtcNow;

        await Task.Delay(_processingDelayMs, ct);

        if (_errorSimulator.ShouldFail(errorPercent))
        {
            throw new InvalidOperationException("Сбой при обработке заявки (симуляция)");
        }

        request.Status = RequestStatus.Completed;
        request.UpdatedAt = DateTime.UtcNow;

        return request;
    }
}
