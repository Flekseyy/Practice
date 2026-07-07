using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface IRetryPolicy
{
    Task<SupportRequest> ExecuteAsync(
        Func<CancellationToken, Task<SupportRequest>> action,
        SupportRequest request,
        int maxRetries,
        TimeSpan delay,
        CancellationToken operationCt,
        CancellationToken retryDelayCt);
}
