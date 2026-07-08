using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Processing;

public class RequestProcessor(IErrorSimulator errorSimulator, int processingDelayMs = 1000)
    : IRequestProcessor
{
    public async Task<SupportRequest> ProcessAsync(SupportRequest request, int errorPercent, CancellationToken ct)
    {
        request.UpdatedAt = DateTime.UtcNow;

        await Task.Delay(processingDelayMs, ct);

        if (errorSimulator.ShouldFail(errorPercent))
        {
            throw new InvalidOperationException("Сбой при обработке заявки (симуляция)");
        }

        request.Status = RequestStatus.Completed;
        request.UpdatedAt = DateTime.UtcNow;

        return request;
    }
}
