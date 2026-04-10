using BOTC.Domain.Users;

namespace BOTC.Domain.Tests.Players;

public sealed class UserTests
{
    [Fact]
    public void Create_WhenValidInput_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var id = UserId.New();

        // Act
        var user = User.Create(id, "alice", "Ali");

        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal("alice", user.Username);
        Assert.Equal("ALICE", user.NormalizedUsername);
        Assert.Equal("Ali", user.NickName);
        Assert.Equal("ALI", user.NormalizedNickName);
    }

    [Fact]
    public void Create_WhenUsernameHasLeadingAndTrailingWhitespace_TrimsIt()
    {
        // Arrange
        var id = UserId.New();

        // Act
        var user = User.Create(id, "  alice  ", "Ali");

        // Assert
        Assert.Equal("alice", user.Username);
    }

    [Fact]
    public void Create_WhenNickNameHasLeadingAndTrailingWhitespace_TrimsIt()
    {
        // Arrange
        var id = UserId.New();

        // Act
        var user = User.Create(id, "alice", "  Ali  ");

        // Assert
        Assert.Equal("Ali", user.NickName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenUsernameIsNullOrEmptyOrWhitespace_ThrowsArgumentException(string? username)
    {
        // Arrange
        var id = UserId.New();

        // Act
        Action act = () => User.Create(id, username!, "Ali");

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNickNameIsNullOrEmptyOrWhitespace_ThrowsArgumentException(string? nickName)
    {
        // Arrange
        var id = UserId.New();

        // Act
        Action act = () => User.Create(id, "alice", nickName!);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenUsernameExceeds50Characters_ThrowsArgumentException()
    {
        // Arrange
        var id = UserId.New();
        var longUsername = new string('a', 51);

        // Act
        Action act = () => User.Create(id, longUsername, "Ali");

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenNickNameExceeds50Characters_ThrowsArgumentException()
    {
        // Arrange
        var id = UserId.New();
        var longNickName = new string('a', 51);

        // Act
        Action act = () => User.Create(id, "alice", longNickName);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenValidInput_NormalizedUsernameIsUppercaseOfUsername()
    {
        // Arrange
        var id = UserId.New();

        // Act
        var user = User.Create(id, "Alice", "Ali");

        // Assert
        Assert.Equal(user.Username.ToUpperInvariant(), user.NormalizedUsername);
    }

    [Fact]
    public void Create_WhenValidInput_NormalizedNickNameIsUppercaseOfNickName()
    {
        // Arrange
        var id = UserId.New();

        // Act
        var user = User.Create(id, "alice", "Ali");

        // Assert
        Assert.Equal(user.NickName.ToUpperInvariant(), user.NormalizedNickName);
    }

    [Fact]
    public void Rehydrate_WhenValidInput_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var id = UserId.New();

        // Act
        var user = User.Rehydrate(id, "alice", "ALICE", "Ali", "ALI");

        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal("alice", user.Username);
        Assert.Equal("ALICE", user.NormalizedUsername);
        Assert.Equal("Ali", user.NickName);
        Assert.Equal("ALI", user.NormalizedNickName);
    }

    [Fact]
    public void Rehydrate_WhenNormalizedUsernameDoesNotMatchUsername_ThrowsArgumentException()
    {
        // Arrange
        var id = UserId.New();

        // Act
        Action act = () => User.Rehydrate(id, "alice", "WRONG", "Ali", "ALI");

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Rehydrate_WhenNormalizedNickNameDoesNotMatchNickName_ThrowsArgumentException()
    {
        // Arrange
        var id = UserId.New();

        // Act
        Action act = () => User.Rehydrate(id, "alice", "ALICE", "Ali", "WRONG");

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void ChangeNickName_WhenValidInput_ReturnsNewUserWithUpdatedNickNameAndNormalizedNickName()
    {
        // Arrange
        var id = UserId.New();
        var user = User.Create(id, "alice", "Ali");

        // Act
        var updated = user.ChangeNickName("Bob");

        // Assert
        Assert.Equal("Bob", updated.NickName);
        Assert.Equal("BOB", updated.NormalizedNickName);
    }

    [Fact]
    public void ChangeNickName_WhenCalled_PreservesIdUsernameAndNormalizedUsername()
    {
        // Arrange
        var id = UserId.New();
        var user = User.Create(id, "alice", "Ali");

        // Act
        var updated = user.ChangeNickName("Bob");

        // Assert
        Assert.Equal(id, updated.Id);
        Assert.Equal("alice", updated.Username);
        Assert.Equal("ALICE", updated.NormalizedUsername);
    }

    [Fact]
    public void ChangeNickName_WhenNickNameHasLeadingAndTrailingWhitespace_TrimsIt()
    {
        // Arrange
        var id = UserId.New();
        var user = User.Create(id, "alice", "Ali");

        // Act
        var updated = user.ChangeNickName("  Bob  ");

        // Assert
        Assert.Equal("Bob", updated.NickName);
    }

    [Fact]
    public void ChangeNickName_WhenNickNameIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var id = UserId.New();
        var user = User.Create(id, "alice", "Ali");

        // Act
        Action act = () => user.ChangeNickName("");

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void ChangeUsername_WhenValidInput_ReturnsNewUserWithUpdatedUsernameAndNormalizedUsername()
    {
        // Arrange
        var id = UserId.New();
        var user = User.Create(id, "alice", "Ali");

        // Act
        var updated = user.ChangeUsername("bob");

        // Assert
        Assert.Equal("bob", updated.Username);
        Assert.Equal("BOB", updated.NormalizedUsername);
    }

    [Fact]
    public void ChangeUsername_WhenCalled_PreservesIdNickNameAndNormalizedNickName()
    {
        // Arrange
        var id = UserId.New();
        var user = User.Create(id, "alice", "Ali");

        // Act
        var updated = user.ChangeUsername("bob");

        // Assert
        Assert.Equal(id, updated.Id);
        Assert.Equal("Ali", updated.NickName);
        Assert.Equal("ALI", updated.NormalizedNickName);
    }

    [Fact]
    public void ChangeUsername_WhenUsernameHasLeadingAndTrailingWhitespace_TrimsIt()
    {
        // Arrange
        var id = UserId.New();
        var user = User.Create(id, "alice", "Ali");

        // Act
        var updated = user.ChangeUsername("  bob  ");

        // Assert
        Assert.Equal("bob", updated.Username);
    }

    [Fact]
    public void ChangeUsername_WhenUsernameIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var id = UserId.New();
        var user = User.Create(id, "alice", "Ali");

        // Act
        Action act = () => user.ChangeUsername("");

        // Assert
        Assert.Throws<ArgumentException>(act);
    }
}

