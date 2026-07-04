using ResponsiveProcessingStudio.Core.Domain;

public class DepositService : BankService
{
    public override ServiceType Type => ServiceType.Deposit;
    public override string DisplayName => "Вклад";
    public override string RequiredDepartment => "Отдел вкладов";
}