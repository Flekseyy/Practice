using ResponsiveProcessingStudio.Core.Domain;

namespace ResponsiveProcessingStudio.Web.Services;

public sealed class TestDataGeneratorService
{
    private static readonly string[] ClientNames =
    [
        "Иван Сидоров",
        "Мария Иванова",
        "Петр Кузнецов",
        "Анна Попова",
        "Сергей Николаев",
        "Ольга Смирнова",
        "Дмитрий Соколов",
        "Елена Васильева"
    ];

    private static readonly (ServiceType Type, string Message)[] Messages =
    [
        (ServiceType.Credit, "Хочу узнать ставку по кредиту и условия досрочного погашения."),
        (ServiceType.DebitCard, "Не проходит оплата картой в интернет-магазине."),
        (ServiceType.Deposit, "Нужна консультация по открытию вклада."),
        (ServiceType.Mortgage, "Есть вопрос по ипотеке и графику платежей."),
        (ServiceType.MoneyTransfer, "Не дошел перевод получателю."),
        (ServiceType.Unknown, "Помогите разобраться с банковской операцией.")
    ];

    public IEnumerable<SupportRequest> CreateRequests(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var message = Messages[Random.Shared.Next(Messages.Length)];

            yield return new SupportRequest
            {
                ClientName = ClientNames[Random.Shared.Next(ClientNames.Length)],
                Message = message.Message,
                ServiceType = Random.Shared.Next(0, 5) == 0 ? ServiceType.Unknown : message.Type,
                Status = RequestStatus.Created,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
