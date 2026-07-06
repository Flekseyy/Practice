using ResponsiveProcessingStudio.Core.Abstractions;

namespace ResponsiveProcessingStudio.Core.Simulation;

public class ErrorSimulator : IErrorSimulator
{
    public bool ShouldFail(int errorPercent)
    {
        return Random.Shared.Next(1, 101) <= errorPercent;
    }
}