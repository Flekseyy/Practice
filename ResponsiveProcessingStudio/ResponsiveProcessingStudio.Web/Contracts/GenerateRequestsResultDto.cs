namespace ResponsiveProcessingStudio.Web.Contracts;

public sealed class GenerateRequestsResultDto
{
    public int Requested { get; init; }
    public int Accepted { get; init; }
    public int Rejected { get; init; }
}
