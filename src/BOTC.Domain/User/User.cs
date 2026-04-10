namespace BOTC.Domain.Users;

public sealed class User
{
    private User(
        UserId id,
        string username,
        string normalizedUsername,
        string nickName,
        string normalizedNickName)
    {
        Id = id;
        Username = username;
        NormalizedUsername = normalizedUsername;
        NickName = nickName;
        NormalizedNickName = normalizedNickName;
    }

    public UserId Id { get; }
    public string Username { get; }
    public string NormalizedUsername { get; }
    public string NickName { get; }
    public string NormalizedNickName { get; }

    public static User Create(
        UserId id,
        string username,
        string nickName)
    {
        var (validatedUsername, normalizedUsername) = ProcessUsername(username);
        var (validatedNickName, normalizedNickName) = ProcessNickName(nickName);

        return new User(
            id,
            validatedUsername,
            normalizedUsername,
            validatedNickName,
            normalizedNickName);
    }

    public static User Rehydrate(
        UserId id,
        string username,
        string normalizedUsername,
        string nickName,
        string normalizedNickName)
    {
        var (validatedUsername, expectedNormalizedUsername) = ProcessUsername(username);
        var (validatedNickName, expectedNormalizedNickName) = ProcessNickName(nickName);

        if (!string.Equals(expectedNormalizedUsername, normalizedUsername, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Persisted normalized username does not match the persisted username.",
                nameof(normalizedUsername));
        }

        if (!string.Equals(expectedNormalizedNickName, normalizedNickName, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Persisted normalized nickname does not match the persisted nickname.",
                nameof(normalizedNickName));
        }

        return new User(
            id,
            validatedUsername,
            expectedNormalizedUsername,
            validatedNickName,
            expectedNormalizedNickName);
    }

    public User ChangeNickName(string nickName)
    {
        var (validatedNickName, normalizedNickName) = ProcessNickName(nickName);

        return new User(
            Id,
            Username,
            NormalizedUsername,
            validatedNickName,
            normalizedNickName);
    }

    public User ChangeUsername(string username)
    {
        var (validatedUsername, normalizedUsername) = ProcessUsername(username);

        return new User(
            Id,
            validatedUsername,
            normalizedUsername,
            NickName,
            NormalizedNickName);
    }

    private static (string Value, string NormalizedValue) ProcessUsername(string username)
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

        return (trimmed, trimmed.ToUpperInvariant());
    }

    private static (string Value, string NormalizedValue) ProcessNickName(string nickName)
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

        return (trimmed, trimmed.ToUpperInvariant());
    }
}