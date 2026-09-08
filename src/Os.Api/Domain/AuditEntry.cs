namespace Os.Api.Domain;

public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceOrderId
    { get; set; }
    public Guid ActorId
    { get; set; }
    public User Actor { get; set; } = null!;
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
