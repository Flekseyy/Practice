using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Retry;

public class RetryPolicy : IRetryPolicy
{
    private readonly IPipelineMetrics _metrics;
    
    public RetryPolicy(IPipelineMetrics metrics)
    {
        _metrics = metrics;
    }

    public async Task<SupportRequest> ExecuteAsync(
        Func<CancellationToken, Task<SupportRequest>> action,
        SupportRequest request,
        int maxRetries,
        TimeSpan delay,
        CancellationToken ct)
    {
        for (int attempt = 0; attempt <= maxRetries + 1; attempt++)
        {
            try
            {
                return await action(ct);
            }
            catch (OperationCanceledException)
            {
                request.Status = RequestStatus.Cancelled;
                request.UpdatedAt = DateTime.UtcNow;
                return request;
            }
            catch (Exception ex)
            {
                request.LastError =  ex.Message;

                if (attempt > maxRetries)
                {
                    request.Status = RequestStatus.Failed;
                    request.UpdatedAt = DateTime.UtcNow;
                    return request;
                }

                request.RetryCount++;
                _metrics.IncrementRetry();
                
                await Task.Delay(delay, ct);
            }
            
        }
        return request;
    }
}