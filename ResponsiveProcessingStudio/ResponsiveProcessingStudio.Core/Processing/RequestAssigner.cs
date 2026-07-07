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

    private static readonly Dictionary<ServiceType, string> Staff = new()
    {
        [ServiceType.Credit] = "А. Смирнов",
        [ServiceType.DebitCard] = "А. Петрова",
        [ServiceType.Deposit] = "Д. Волков",
        [ServiceType.Mortgage] = "Е. Васильева",
        [ServiceType.MoneyTransfer] = "И. Федоров",
        [ServiceType.Unknown] = "Дежурный специалист"
    };

    public Task<SupportRequest> AssignAsync(SupportRequest request, CancellationToken ct)
    {
        request.AssignedDepartment = request.ServiceType == ServiceType.Unknown
            ? "Общий отдел"
            : _factory.Create(request.ServiceType).RequiredDepartment;

        request.AssignedHandler = Staff[request.ServiceType];
        request.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(request);
    }
}
