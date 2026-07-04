namespace ResponsiveProcessingStudio.Core.Domain;

public class DebitCardService : BankService
{
    public override ServiceType Type => ServiceType.DebitCard;
    public override string DisplayName => "Банковская карта";
    public override string RequiredDepartment => "Карточный отдел";
}