using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Domain;
using ResponsiveProcessingStudio.Core.Factories;

namespace ResponsiveProcessingStudio.Core.Processing;

public class RequestAssigner : IRequestAssigner
{
    private readonly BankServiceFactory _factory;
    
    private static readonly Dictionary<ServiceType, string[]> Staff = new()
    {
        [ServiceType.Credit]        = new[] { "Иван", "Мария" },
        [ServiceType.DebitCard]     = new[] { "Алексей", "Ольга" },
        [ServiceType.Deposit]       = new[] { "Дмитрий" },
        [ServiceType.Mortgage]      = new[] { "Светлана" },
        [ServiceType.MoneyTransfer] = new[] { "Павел" },
        [ServiceType.Unknown]       = new[] { "Дежурный специалист" }
    };

    public RequestAssigner(BankServiceFactory factory)
    {
        _factory = factory;
    }

    public Task<SupportRequest> AssignAsync(SupportRequest request, CancellationToken ct)
    {
        request.AssignedDepartment = request.ServiceType == ServiceType.Unknown
            ? "Общий отдел"
            : _factory.Create(request.ServiceType).RequiredDepartment;

        var staff = Staff[request.ServiceType];
        request.AssignedHandler = staff[Random.Shared.Next(staff.Length)];
        
        request.Status = RequestStatus.Assigning;
        request.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(request);
    }
}