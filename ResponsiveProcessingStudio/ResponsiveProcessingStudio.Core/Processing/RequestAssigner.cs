using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Factories;

namespace ResponsiveProcessingStudio.Core.Processing;

public class RequestAssigner : IRequestAssigner
{
    private readonly BankServiceFactory _factory;
    public RequestAssigner(BankServiceFactory factory)
    {
        _factory = factory;
    }
 // Hardcode   
    private static readonly Dictionary<ServiceType, string> Staff = new()
    {
        [ServiceType.Credit]        = "Отдел кредитования",
        [ServiceType.DebitCard]     = "Отдел по платежным картам" ,
        [ServiceType.Deposit]       = "Отдел сберегательных счетов" ,
        [ServiceType.Mortgage]      = "Отдел ипотечного кредитования" ,
        [ServiceType.MoneyTransfer] = "Отдел по банковским операциям" ,
        [ServiceType.Unknown]       = "Дежурный специалист" 
    };

    public Task<SupportRequest> AssignAsync(SupportRequest request, CancellationToken ct)
    {
        request.AssignedDepartment = request.ServiceType == ServiceType.Unknown
            ? "Общий отдел"
            : _factory.Create(request.ServiceType).RequiredDepartment;

        request.AssignedHandler = Staff[request.ServiceType];
        
        request.Status = RequestStatus.Assigning;
        request.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(request);
    }
}