using ResponsiveProcessingStudio.Core.Domain;

public class CreditService : BankService
{
    public override ServiceType Type => ServiceType.Credit;
    public override string DisplayName => "Кредит";
    public override string RequiredDepartment => "Кредитный отдел";
}