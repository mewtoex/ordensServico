namespace Os.Api.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role
    { get; set; }
    public Guid SecurityVersion { get; set; } = Guid.NewGuid();
    public byte[] Version { get; set; } = [];
    public bool Active { get; set; } = true;
}
