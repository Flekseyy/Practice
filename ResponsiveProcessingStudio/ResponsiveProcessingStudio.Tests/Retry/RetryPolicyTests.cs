using FluentAssertions;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Pipeline;
using ResponsiveProcessingStudio.Core.Retry;

namespace ResponsiveProcessingStudio.Tests.Retry;

//заглушка для метрик чтобы не тащить реальный ConcurrentDictionary
public class FakePipelineMetrics : IPipelineMetrics
{
    public int RetryCalls { get; private set; }
    public void IncrementRetry() => RetryCalls++;
    
    // методы интерфейса можно оставить пустыми т.к. в тестах они не вызываются напрямую извне
    public PipelineSnapshot GetSnapshot() => throw new NotImplementedException();
    public void OnStatusChanged(RequestStatus oldStatus, RequestStatus newStatus) { }
}

public class RetryPolicyTests
{
    private readonly FakePipelineMetrics _metrics = new();
    private readonly RetryPolicy _policy;
    private readonly CancellationToken _ct = CancellationToken.None;

    public RetryPolicyTests()
    {
        _policy = new RetryPolicy(_metrics);
    }

    [Fact]
    public async Task ExecuteAsync_WhenActionSucceeds_ShouldReturnResultWithoutRetries()
    {
        var request = new SupportRequest { Message = "Test" };
        int callCount = 0;
        
        Func<CancellationToken, Task<SupportRequest>> action = ct =>
        {
            callCount++;
            request.Status = RequestStatus.Completed;
            return Task.FromResult(request);
        };
        
        var result = await _policy.ExecuteAsync(action, request, maxRetries: 3, TimeSpan.Zero, _ct, _ct);
        
        result.Status.Should().Be(RequestStatus.Completed);
        callCount.Should().Be(1);  
        _metrics.RetryCalls.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenActionFailsThenSucceeds_ShouldRetryAndIncrementMetric()
    {
        var request = new SupportRequest { Message = "Test" };
        int callCount = 0;
        
        Func<CancellationToken, Task<SupportRequest>> action = ct =>
        {
            callCount++;
            if (callCount < 3) throw new InvalidOperationException("Simulated error");
            
            request.Status = RequestStatus.Completed;
            return Task.FromResult(request);
        };
        
        var result = await _policy.ExecuteAsync(action, request, maxRetries: 3, TimeSpan.Zero, _ct, _ct);

        result.Status.Should().Be(RequestStatus.Completed);
        callCount.Should().Be(3); // 1 попытка + 2 retry
        result.RetryCount.Should().Be(2);
        _metrics.RetryCalls.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAllRetriesExhausted_ShouldReturnFailed()
    {
        var request = new SupportRequest { Message = "Test" };
        Func<CancellationToken, Task<SupportRequest>> action = ct => 
            throw new InvalidOperationException("Permanent error");
        
        var result = await _policy.ExecuteAsync(action, request, maxRetries: 2, TimeSpan.Zero, _ct, _ct);
        
        result.Status.Should().Be(RequestStatus.Failed);
        result.RetryCount.Should().Be(2);
        result.LastError.Should().Contain("Permanent error");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledDuringRetryDelay_ShouldReturnCancelled()
    {
        var request = new SupportRequest { Message = "Test" };
        var cts = new CancellationTokenSource();
        
        Func<CancellationToken, Task<SupportRequest>> action = ct => 
            throw new InvalidOperationException("Error to trigger retry");

        // Отменяем токен retryDelayCt сразу после первого падения
        var retryDelayCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            retryDelayCts.Cancel();
        });
        
        var result = await _policy.ExecuteAsync(
            action, request, maxRetries: 5, TimeSpan.FromSeconds(10), 
            cts.Token, retryDelayCts.Token);
        
        result.Status.Should().Be(RequestStatus.Cancelled);
    }
}