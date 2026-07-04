namespace ResponsiveProcessingStudio.Core.Domain;

public abstract class BankService
{
    public abstract ServiceType Type { get; }
    public abstract string DisplayName { get; }
    public abstract string RequiredDepartment { get; }
}