using Microsoft.AspNetCore.Mvc;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Web.Contracts;
using ResponsiveProcessingStudio.Web.Services;

namespace ResponsiveProcessingStudio.Web.Controllers;

[ApiController]
[Route("api/pipeline")]
public sealed class PipelineController : ControllerBase
{
    private readonly ISupportRequestPipeline _pipeline;
    private readonly TestDataGeneratorService _generator;
    private readonly IHostApplicationLifetime _appLifetime;

    public PipelineController(
        ISupportRequestPipeline pipeline,
        TestDataGeneratorService generator,
        IHostApplicationLifetime appLifetime)
    {
        _pipeline = pipeline;
        _generator = generator;
        _appLifetime = appLifetime;
    }

    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(PipelineSnapshotDto), StatusCodes.Status200OK)]
    public ActionResult<PipelineSnapshotDto> GetSnapshot()
    {
        return Ok(PipelineSnapshotDto.FromDomain(_pipeline.GetSnapshot()));
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(PipelineSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PipelineSnapshotDto>> StartAsync(PipelineOptionsDto options)
    {
        await _pipeline.StartAsync(options.ToPipelineOptions(), _appLifetime.ApplicationStopping);
        return Ok(PipelineSnapshotDto.FromDomain(_pipeline.GetSnapshot()));
    }

    [HttpPost("stop")]
    [ProducesResponseType(typeof(PipelineSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PipelineSnapshotDto>> StopAsync()
    {
        await _pipeline.StopAsync();
        return Ok(PipelineSnapshotDto.FromDomain(_pipeline.GetSnapshot()));
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateRequestsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GenerateRequestsResultDto>> GenerateAsync([FromQuery] int count = 100, CancellationToken ct = default)
    {
        var snapshot = _pipeline.GetSnapshot();
        if (!string.Equals(snapshot.PipelineStatus, "Running", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = "Pipeline is not running. Start it before generating requests." });
        }

        var safeCount = Math.Clamp(count, 1, 1_000);
        var accepted = 0;

        foreach (var request in _generator.CreateRequests(safeCount))
        {
            if (await _pipeline.SendAsync(request, ct))
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
