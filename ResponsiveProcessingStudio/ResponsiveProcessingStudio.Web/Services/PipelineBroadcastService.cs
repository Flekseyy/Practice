using Microsoft.AspNetCore.SignalR;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Web.Contracts;
using ResponsiveProcessingStudio.Web.Hubs;

namespace ResponsiveProcessingStudio.Web.Services;

public sealed class PipelineBroadcastService(
    ISupportRequestPipeline pipeline,
    IHubContext<PipelineHub> hubContext,
    ILogger<PipelineBroadcastService> logger)
    : BackgroundService
{
    private static readonly TimeSpan BroadcastInterval = TimeSpan.FromMilliseconds(400);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(BroadcastInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var snapshot = PipelineSnapshotDto.FromDomain(pipeline.GetSnapshot());
                await hubContext.Clients.All.SendAsync("pipelineSnapshot", snapshot, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to broadcast pipeline snapshot.");
            }
        }
    }
}
