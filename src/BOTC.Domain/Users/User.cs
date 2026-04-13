namespace BOTC.Domain.Users;

public sealed class User
{
    private User(
        UserId id,
        string username,
        string nickName)
    {
        Id = id;
        Username = username;
        NickName = nickName;
    }

    public UserId Id { get; }
    public string Username { get; }
    public string NickName { get; }

    public string NormalizedUsername => Normalize(Username);
    public string NormalizedNickName => Normalize(NickName);

    public static User Create(
        UserId id,
        string username,
        string nickName)
    {
        return new User(
            id,
            ValidateUsername(username),
            ValidateNickName(nickName));
    }

    public static User Rehydrate(
        UserId id,
        string username,
        string nickName)
    {
        return new User(
            id,
            ValidateUsername(username),
            ValidateNickName(nickName));
    }

    public User ChangeNickName(string nickName)
    {
        return new User(
            Id,
            Username,
            ValidateNickName(nickName));
    }

    public User ChangeUsername(string username)
    {
        return new User(
            Id,
            ValidateUsername(username),
            NickName);
    }

    private static string ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        var trimmed = username.Trim();

        if (trimmed.Length > 50)
        {
            throw new ArgumentException("Username must not exceed 50 characters.", nameof(username));
        }

        return trimmed;
    }

    private static string ValidateNickName(string nickName)
    {
        if (string.IsNullOrWhiteSpace(nickName))
        {
            throw new ArgumentException("Nickname is required.", nameof(nickName));
        }

        var trimmed = nickName.Trim();

        if (trimmed.Length > 50)
        {
            throw new ArgumentException("Nickname must not exceed 50 characters.", nameof(nickName));
        }

        return trimmed;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}