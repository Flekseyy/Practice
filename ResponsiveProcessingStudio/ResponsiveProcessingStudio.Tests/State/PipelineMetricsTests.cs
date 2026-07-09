using FluentAssertions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.State;

namespace ResponsiveProcessingStudio.Tests.State;

public class PipelineMetricsTests
{
    private readonly PipelineMetrics _metrics = new();

    [Fact]
    public void OnStatusChanged_ShouldDecrementOldAndIncrementNew()
    {
        _metrics.OnStatusChanged(RequestStatus.Created, RequestStatus.Waiting);
        _metrics.OnStatusChanged(RequestStatus.Waiting, RequestStatus.Classifying);

        var snapshot = _metrics.GetSnapshot();
        snapshot.WaitingCount.Should().Be(0);
        snapshot.ClassifyingCount.Should().Be(1);
    }

    [Fact]
    public void IncrementRetry_ShouldIncreaseRetryCountInSnapshot()
    {
        _metrics.IncrementRetry();
        _metrics.IncrementRetry();
        
        var snapshot = _metrics.GetSnapshot();
        snapshot.RetryCount.Should().Be(2);
    }

    [Fact]
    public void GetSnapshot_ShouldReturnZeroForUninitializedStatuses()
    {
        var snapshot = _metrics.GetSnapshot();
        
        snapshot.CompletedCount.Should().Be(0);
        snapshot.FailedCount.Should().Be(0);
    }
}