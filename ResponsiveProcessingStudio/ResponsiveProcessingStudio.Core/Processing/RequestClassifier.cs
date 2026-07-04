using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Core.Processing;

public class RequestClassifier : IRequestClassifier
{
    private static readonly Dictionary<ServiceType, string[]> Keywords = new()
    {
        [ServiceType.Credit]        = ["кредит", "займ", "ставк"],
        [ServiceType.DebitCard]     = ["карт", "оплатить картой"],
        [ServiceType.Deposit]       = ["вклад", "депозит"],
        [ServiceType.Mortgage]      = ["ипотек"],
        [ServiceType.MoneyTransfer] = ["перевод", "не дошёл", "перевести"]
    };

    public Task<SupportRequest> ClassifyAsync(SupportRequest request, CancellationToken ct)
    {
        if (request.ServiceType == ServiceType.Unknown)
        {
            var text = request.Message.ToLowerInvariant();
            var match = Keywords.FirstOrDefault(kv => kv.Value.Any(text.Contains));
            request.ServiceType = match.Key == default && match.Value == null
                ? ServiceType.Unknown
                : match.Key;
        }
        
        request.Status = RequestStatus.Classifying;
        request.UpdatedAt = DateTime.UtcNow;
        
        return Task.FromResult(request);
    }
}