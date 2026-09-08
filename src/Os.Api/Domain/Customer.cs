namespace Os.Api.Domain;

public class Customer
{
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}
