using Microsoft.AspNetCore.SignalR;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Web.Contracts;
using ResponsiveProcessingStudio.Web.Hubs;

namespace ResponsiveProcessingStudio.Web.Services;

public sealed class PipelineBroadcastService : BackgroundService
{
    private static readonly TimeSpan BroadcastInterval = TimeSpan.FromMilliseconds(400);

    private readonly ISupportRequestPipeline _pipeline;
    private readonly IHubContext<PipelineHub> _hubContext;
    private readonly ILogger<PipelineBroadcastService> _logger;

    public PipelineBroadcastService(
        ISupportRequestPipeline pipeline,
        IHubContext<PipelineHub> hubContext,
        ILogger<PipelineBroadcastService> logger)
    {
        _pipeline = pipeline;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(BroadcastInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var snapshot = PipelineSnapshotDto.FromDomain(_pipeline.GetSnapshot());
                await _hubContext.Clients.All.SendAsync("pipelineSnapshot", snapshot, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast pipeline snapshot.");
            }
        }
    }
}
