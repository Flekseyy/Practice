using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Retry;

public class RetryPolicy(IPipelineMetrics metrics) : IRetryPolicy
{
    public async Task<SupportRequest> ExecuteAsync(
        Func<CancellationToken, Task<SupportRequest>> action,
        SupportRequest request,
        int maxRetries,
        TimeSpan delay,
        CancellationToken operationCt,
        CancellationToken retryDelayCt)
    {
        for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
        {
            try
            {
                var result = await action(operationCt);
                result.LastError = null;
                return result;
            }
            catch (OperationCanceledException)
            {
                request.Status = RequestStatus.Cancelled;
                request.UpdatedAt = DateTime.UtcNow;
                return request;
            }
            catch (Exception ex)
            {
                request.LastError = ex.Message;

                if (attempt > maxRetries)
                {
                    request.Status = RequestStatus.Failed;
                    request.UpdatedAt = DateTime.UtcNow;
                    return request;
                }

                request.RetryCount++;
                metrics.IncrementRetry();

                try
                {
                    await Task.Delay(delay, retryDelayCt);
                }
                catch (OperationCanceledException)
                {
                    request.Status = RequestStatus.Cancelled;
                    request.UpdatedAt = DateTime.UtcNow;
                    return request;
                }
            }
        }

        return request;
    }
}
