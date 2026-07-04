using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Processing;

public class RequestProcessor : IRequestProcessor
{
    private readonly IErrorSimulator _errorSimulator;
    private readonly int _errorPercent;
    private readonly int _processingDelayMs;

    public RequestProcessor(IErrorSimulator errorSimulator, int errorPercent = 20, int processingDelayMs = 1000)
    {
        _errorSimulator = errorSimulator;
        _errorPercent = errorPercent;
        _processingDelayMs = processingDelayMs;
    }

    public async Task<SupportRequest> ProcessAsync(SupportRequest request, CancellationToken ct)
    {
        request.Status = RequestStatus.Processing;
        request.UpdatedAt = DateTime.UtcNow;

        await Task.Delay(_processingDelayMs, ct);

        if (_errorSimulator.ShouldFail(_errorPercent))
        {
            throw new InvalidOperationException("Сбой при обработке заявки (симуляция)");
        }

        request.Status = RequestStatus.Completed;
        request.UpdatedAt = DateTime.UtcNow;

        return request;
    }
}