using ResponsiveProcessingStudioAPI.Models.Enums;

namespace ResponsiveProcessingStudioAPI.Models;

public abstract class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public abstract TicketType Type { get; } 
        
    public PriorityLevel Priority { get; set; } = PriorityLevel.Regular;
    public TicketStatus Status { get; set; } = TicketStatus.Received;
        
    public string? Assignee { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}