using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Processing;

public class RequestClassifier : IRequestClassifier
{
    private static readonly Dictionary<ServiceType, string[]> Keywords = new()
    {
        [ServiceType.Credit] = ["кредит", "займ", "ставк", "платеж по кредиту"],
        [ServiceType.DebitCard] = ["карт", "оплатить картой", "банкомат", "пин"],
        [ServiceType.Deposit] = ["вклад", "депозит", "накопительн"],
        [ServiceType.Mortgage] = ["ипотек"],
        [ServiceType.MoneyTransfer] = ["перевод", "не дошел", "не дошёл", "перевести", "получател"]
    };

    public Task<SupportRequest> ClassifyAsync(SupportRequest request, CancellationToken ct)
    {
        if (request.ServiceType == ServiceType.Unknown)
        {
            var match = Keywords.FirstOrDefault(kv => 
                kv.Value.Any(keyword => request.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            
            request.ServiceType = match.Key == default && match.Value == null
                ? ServiceType.Unknown
                : match.Key;
        }
        
        request.UpdatedAt = DateTime.UtcNow;
        
        return Task.FromResult(request);
    }
}
