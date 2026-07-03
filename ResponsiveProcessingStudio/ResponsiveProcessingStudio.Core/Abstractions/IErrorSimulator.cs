namespace ResponsiveProcessingStudio.Core.Abstractions;

public interface IErrorSimulator
{
    bool ShouldFail(int errorPercent);
}