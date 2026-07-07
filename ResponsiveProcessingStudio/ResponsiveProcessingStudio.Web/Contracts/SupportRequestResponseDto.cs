using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Web.Contracts;

public sealed class SupportRequestResponseDto
{
    public Guid Id { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public ServiceType ServiceType { get; init; }
    public RequestStatus Status { get; init; }
    public int RetryCount { get; init; }
    public string? AssignedDepartment { get; init; }
    public string? AssignedHandler { get; init; }
    public string? LastError { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static SupportRequestResponseDto FromDomain(SupportRequest request)
    {
        return new SupportRequestResponseDto
        {
            Id = request.Id,
            ClientName = request.ClientName,
            Message = request.Message,
            ServiceType = request.ServiceType,
            Status = request.Status,
            RetryCount = request.RetryCount,
            AssignedDepartment = request.AssignedDepartment,
            AssignedHandler = request.AssignedHandler,
            LastError = request.LastError,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }
}
