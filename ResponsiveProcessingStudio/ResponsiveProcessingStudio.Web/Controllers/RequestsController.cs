using Microsoft.AspNetCore.Mvc;
using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Web.Contracts;

namespace ResponsiveProcessingStudio.Web.Controllers;

[ApiController]
[Route("api/requests")]
public sealed class RequestsController(ISupportRequestPipeline pipeline, IRequestStateStore stateStore)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SupportRequestResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateAsync(CreateRequestDto dto, CancellationToken ct)
    {
        var request = new SupportRequest
        {
            ClientName = dto.ClientName.Trim(),
            Message = dto.Message.Trim(),
            ServiceType = dto.ServiceType ?? ServiceType.Unknown,
            Status = RequestStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        var accepted = await pipeline.SendAsync(request, ct);
        if (!accepted)
        {
            var snapshot = pipeline.GetSnapshot();
            if (!string.Equals(snapshot.PipelineStatus, "Running", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = "Pipeline is not running. Start it before creating requests." });
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Request queue is full. Try again later." });
        }

        return Accepted($"/api/requests/{request.Id}", SupportRequestResponseDto.FromDomain(request));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SupportRequestResponseDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<SupportRequestResponseDto>> GetAll([FromQuery] RequestStatus? status = null)
    {
        IEnumerable<SupportRequest> requests = stateStore.GetAll();

        if (status.HasValue)
        {
            requests = requests.Where(request => request.Status == status.Value);
        }

        var result = requests
            .OrderByDescending(request => request.UpdatedAt ?? request.CreatedAt)
            .Select(SupportRequestResponseDto.FromDomain)
            .ToArray();

        return Ok(result);
    }

    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SupportRequestResponseDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<SupportRequestResponseDto>> GetRecent([FromQuery] int count = 20)
    {
        var safeCount = Math.Clamp(count, 1, 100);
        var requests = stateStore.GetRecent(safeCount)
            .Select(SupportRequestResponseDto.FromDomain)
            .ToArray();

        return Ok(requests);
    }
}
