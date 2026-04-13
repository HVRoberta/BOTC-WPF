namespace BOTC.Domain.Users.Exceptions;

/// <summary>
/// Raised when a user cannot be created because the requested username is already taken.
/// </summary>
public sealed class UserUsernameConflictException : InvalidOperationException
{
    public UserUsernameConflictException(string username)
        : base($"Username '{username}' is already taken.")
    {
        Username = username;
    }

    public string Username { get; }
}

