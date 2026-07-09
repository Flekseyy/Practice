using FluentAssertions;
using NSubstitute;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Factories;
using ResponsiveProcessingStudio.Core.Pipeline;
using ResponsiveProcessingStudio.Core.Processing;
using ResponsiveProcessingStudio.Core.Retry;
using ResponsiveProcessingStudio.Core.Simulation;
using ResponsiveProcessingStudio.Core.State;

namespace ResponsiveProcessingStudio.Tests.Pipeline;

public class SupportRequestPipelineTests
{
    private readonly IRequestClassifier _classifier = new RequestClassifier();
    private readonly IRequestValidator _validator;
    private readonly IRequestAssigner _assigner = new RequestAssigner(new BankServiceFactory());
    private readonly IRequestProcessor _processor;
    private readonly IRequestStateStore _stateStore = new RequestStateStore();
    private readonly IPipelineMetrics _metrics = new PipelineMetrics();
    private readonly IRetryPolicy _retryPolicy;
    private readonly SupportRequestPipeline _pipeline;
    private readonly CancellationTokenSource _appLifetimeCts = new();

    public SupportRequestPipelineTests()
    {
        // детерминированные симуляторы 
        var errorSimulator = Substitute.For<IErrorSimulator>();
        errorSimulator.ShouldFail(Arg.Any<int>()).Returns(false);
        
        _validator = new RequestValidator(errorSimulator);
        _processor = new RequestProcessor(errorSimulator, processingDelayMs: 50); 
        
        _retryPolicy = new RetryPolicy(_metrics);
        
        _pipeline = new SupportRequestPipeline(
            _classifier, _validator, _assigner, _processor, 
            _stateStore, _metrics, _retryPolicy);
    }

    [Fact]
    public async Task StartAsync_WhenCalled_ShouldSetRunningStatus()
    {
        var options = new PipelineOptions { WorkersCount = 2, QueueSize = 10 };
        
        await _pipeline.StartAsync(options, _appLifetimeCts.Token);
        
        var snapshot = _pipeline.GetSnapshot();
        snapshot.PipelineStatus.Should().Be("Running");
    }

    [Fact]
    public async Task SendAsync_WhenPipelineIsRunning_ShouldAcceptRequest()
    {
        await _pipeline.StartAsync(new PipelineOptions(), _appLifetimeCts.Token);
        var request = new SupportRequest { ClientName = "Иван", Message = "Тест" };
        
        var accepted = await _pipeline.SendAsync(request, CancellationToken.None);
        
        accepted.Should().BeTrue();
        request.Status.Should().Be(RequestStatus.Waiting);
    }

    [Fact]
    public async Task SendAsync_WhenPipelineIsStopped_ShouldRejectRequest()
    {
        var request = new SupportRequest { ClientName = "Иван", Message = "Тест" };
        
        var accepted = await _pipeline.SendAsync(request, CancellationToken.None);
        
        accepted.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_WhenCalled_ShouldGracefullyStopPipeline()
    {
        await _pipeline.StartAsync(new PipelineOptions(), _appLifetimeCts.Token);
        await _pipeline.SendAsync(new SupportRequest { ClientName = "Иван", Message = "Тест" }, CancellationToken.None);
        
        await _pipeline.StopAsync();
        
        var snapshot = _pipeline.GetSnapshot();
        snapshot.PipelineStatus.Should().Be("Stopped");
    }

    [Fact]
    public async Task StartAsync_WhenCalledTwice_ShouldRebuildGraphWithNewSettings()
    {
        var firstOptions = new PipelineOptions { WorkersCount = 2 };
        var secondOptions = new PipelineOptions { WorkersCount = 8 };
        
        await _pipeline.StartAsync(firstOptions, _appLifetimeCts.Token);
        
        await _pipeline.StartAsync(secondOptions, _appLifetimeCts.Token);
        
        var snapshot = _pipeline.GetSnapshot();
        snapshot.WorkersCount.Should().Be(8);
        snapshot.PipelineStatus.Should().Be("Running");
    }

    public void Dispose()
    {
        _appLifetimeCts.Dispose();
        _pipeline.Dispose();
    }
}