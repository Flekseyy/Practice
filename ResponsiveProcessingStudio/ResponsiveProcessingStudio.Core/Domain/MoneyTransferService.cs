namespace ResponsiveProcessingStudio.Core.Domain;

public class MortgageService : BankService
{
    public override ServiceType Type => ServiceType.Mortgage;
    public override string DisplayName => "Ипотека";
    public override string RequiredDepartment => "Ипотечный отдел";
}