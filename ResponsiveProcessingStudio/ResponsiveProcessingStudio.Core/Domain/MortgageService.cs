namespace ResponsiveProcessingStudio.Core.Domain;

public class MoneyTransferService : BankService
{
    public override ServiceType Type => ServiceType.MoneyTransfer;
    public override string DisplayName => "Денежный перевод";
    public override string RequiredDepartment => "Отдел переводов";
}