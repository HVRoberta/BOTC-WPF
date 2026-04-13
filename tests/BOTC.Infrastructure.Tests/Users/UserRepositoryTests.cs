using BOTC.Domain.Users;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Entities;
using BOTC.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Tests.Users;

public sealed class UserRepositoryTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly BotcDbContext dbContext;
    private readonly UserRepository repository;

    public UserRepositoryTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BotcDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new BotcDbContext(options);
        dbContext.Database.EnsureCreated();

        repository = new UserRepository(dbContext);
    }

    // -------------------------------------------------------------------------
    // Constructor guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WhenDbContextIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new UserRepository(null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("dbContext", exception.ParamName);
    }

    // -------------------------------------------------------------------------
    // TryAddAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TryAddAsync_WhenUsernameIsUnique_ReturnsTrueAndPersistsUser()
    {
        var user = User.Create(UserId.New(), "alice", "Alice");

        var result = await repository.TryAddAsync(user, CancellationToken.None);

        Assert.True(result);
        var persisted = await dbContext.Users.SingleAsync(u => u.Id == user.Id.Value);
        Assert.Equal("alice", persisted.Username);
        Assert.Equal("Alice", persisted.NickName);
        Assert.NotEqual(default, persisted.CreatedAtUtc);
        Assert.NotEqual(default, persisted.UpdatedAtUtc);
    }

    [Fact]
    public async Task TryAddAsync_WhenUsernameAlreadyExists_ReturnsFalse()
    {
        var first = User.Create(UserId.New(), "alice", "Alice");
        var duplicate = User.Create(UserId.New(), "alice", "Alice2");

        await repository.TryAddAsync(first, CancellationToken.None);
        var result = await repository.TryAddAsync(duplicate, CancellationToken.None);

        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsMappedDomainUser()
    {
        var userId = await SeedUserAsync("bob", "Bob");

        var result = await repository.GetByIdAsync(userId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);
        Assert.Equal("bob", result.Username);
        Assert.Equal("Bob", result.NickName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        var result = await repository.GetByIdAsync(UserId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserReturned_DoesNotLeaveTrackedEntities()
    {
        var userId = await SeedUserAsync("bob", "Bob");

        _ = await repository.GetByIdAsync(userId, CancellationToken.None);

        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    // -------------------------------------------------------------------------
    // GetByUsernameAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByUsernameAsync_WhenUserExists_ReturnsMappedDomainUser()
    {
        var userId = await SeedUserAsync("carol", "Carol");

        var result = await repository.GetByUsernameAsync("carol", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);
        Assert.Equal("carol", result.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        var result = await repository.GetByUsernameAsync("nobody", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_WhenUsernameHasLeadingAndTrailingWhitespace_TrimsAndFindsUser()
    {
        await SeedUserAsync("dave", "Dave");

        var result = await repository.GetByUsernameAsync("  dave  ", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("dave", result!.Username);
    }

    // -------------------------------------------------------------------------
    // TrySaveAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TrySaveAsync_WhenUserExists_PersistsUpdatedFields()
    {
        var userId = await SeedUserAsync("eve", "Eve");
        var user = User.Create(userId, "eve", "Eve Updated");

        var saved = await repository.TrySaveAsync(user, CancellationToken.None);

        Assert.True(saved);
        var persisted = await dbContext.Users.SingleAsync(u => u.Id == userId.Value);
        Assert.Equal("Eve Updated", persisted.NickName);
    }

    [Fact]
    public async Task TrySaveAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        var user = User.Create(UserId.New(), "ghost", "Ghost");

        var act = async () => await repository.TrySaveAsync(user, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // -------------------------------------------------------------------------
    // TryDeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TryDeleteAsync_WhenUserExists_ReturnsTrueAndRemovesUser()
    {
        var userId = await SeedUserAsync("frank", "Frank");

        var deleted = await repository.TryDeleteAsync(userId, CancellationToken.None);

        Assert.True(deleted);
        Assert.False(await dbContext.Users.AnyAsync(u => u.Id == userId.Value));
    }

    [Fact]
    public async Task TryDeleteAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        var deleted = await repository.TryDeleteAsync(UserId.New(), CancellationToken.None);

        Assert.False(deleted);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<UserId> SeedUserAsync(string username, string nickName)
    {
        var userId = UserId.New();
        dbContext.Users.Add(new UserEntity
        {
            Id = userId.Value,
            Username = username,
            NickName = nickName,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        return userId;
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
    }
}

