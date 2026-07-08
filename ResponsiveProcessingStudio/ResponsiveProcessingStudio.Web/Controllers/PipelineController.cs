using Microsoft.AspNetCore.Mvc;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Web.Contracts;
using ResponsiveProcessingStudio.Web.Services;

namespace ResponsiveProcessingStudio.Web.Controllers;

[ApiController]
[Route("api/pipeline")]
public sealed class PipelineController(
    ISupportRequestPipeline pipeline,
    TestDataGeneratorService generator,
    IHostApplicationLifetime appLifetime)
    : ControllerBase
{
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(PipelineSnapshotDto), StatusCodes.Status200OK)]
    public ActionResult<PipelineSnapshotDto> GetSnapshot()
    {
        return Ok(PipelineSnapshotDto.FromDomain(pipeline.GetSnapshot()));
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(PipelineSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PipelineSnapshotDto>> StartAsync(PipelineOptionsDto options)
    {
        await pipeline.StartAsync(options.ToPipelineOptions(), appLifetime.ApplicationStopping);
        return Ok(PipelineSnapshotDto.FromDomain(pipeline.GetSnapshot()));
    }

    [HttpPost("stop")]
    [ProducesResponseType(typeof(PipelineSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PipelineSnapshotDto>> StopAsync()
    {
        await pipeline.StopAsync();
        return Ok(PipelineSnapshotDto.FromDomain(pipeline.GetSnapshot()));
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateRequestsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GenerateRequestsResultDto>> GenerateAsync([FromQuery] int count = 100, CancellationToken ct = default)
    {
        var snapshot = pipeline.GetSnapshot();
        if (!string.Equals(snapshot.PipelineStatus, "Running", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = "Pipeline is not running. Start it before generating requests." });
        }

        var safeCount = Math.Clamp(count, 1, 1_000);
        var accepted = 0;

        foreach (var request in generator.CreateRequests(safeCount))
        {
            if (await pipeline.SendAsync(request, ct))
            {
                accepted++;
            }
        }

        return Ok(new GenerateRequestsResultDto
        {
            Requested = safeCount,
            Accepted = accepted,
            Rejected = safeCount - accepted
        });
    }
}
