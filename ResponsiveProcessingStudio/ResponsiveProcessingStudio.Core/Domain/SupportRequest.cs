namespace ResponsiveProcessingStudio.Core.Domain;

public class SupportRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ClientName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public ServiceType ServiceType { get; set; } = ServiceType.Unknown;

    public RequestStatus Status { get; set; } = RequestStatus.Created;

    public int RetryCount { get; set; }

    public string? AssignedDepartment { get; set; }

    public string? AssignedHandler { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
    