namespace BOTC.Infrastructure.Persistence.User;

public sealed class UserEntity
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string NickName { get; set; } = string.Empty;
    
    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}