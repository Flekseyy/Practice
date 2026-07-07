using System.ComponentModel.DataAnnotations;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Web.Contracts;

public sealed class CreateRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string ClientName { get; init; } = string.Empty;

    public ServiceType? ServiceType { get; init; }

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string Message { get; init; } = string.Empty;
}
