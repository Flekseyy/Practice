using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Factories;

public class BankServiceFactory
{
    public BankService Create(ServiceType type) => type switch
    {
        ServiceType.Credit => new CreditService(),
        ServiceType.DebitCard => new DebitCardService(),
        ServiceType.Deposit => new DepositService(),
        ServiceType.Mortgage => new MortgageService(),
        ServiceType.MoneyTransfer => new MoneyTransferService(),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}