using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Features.Users.CreateUser;
using BOTC.Domain.Users;

namespace BOTC.Application.Tests.Features.Users.CreateUser;

public sealed class CreateUserHandlerTests
{
    // -------------------------------------------------------------------------
    // Constructor guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new CreateUserHandler(null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("userRepository", exception.ParamName);
    }

    // -------------------------------------------------------------------------
    // HandleAsync guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        var handler = new CreateUserHandler(new FakeUserRepository());

        var act = async () => await handler.HandleAsync(null!, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(act);
        Assert.Equal("command", exception.ParamName);
    }

    // -------------------------------------------------------------------------
    // HandleAsync scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenUsernameIsAlreadyTaken_ThrowsUserAlreadyExistsException()
    {
        var repository = new FakeUserRepository(tryAddResult: false);
        var handler = new CreateUserHandler(repository);

        var act = async () => await handler.HandleAsync(
            new CreateUserCommand("existinguser", "Nick"),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UserAlreadyExistsException>(act);
        Assert.Equal("existinguser", exception.Username);
        Assert.Contains("existinguser", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNew_CreatesAndReturnsUser()
    {
        var repository = new FakeUserRepository();
        var handler = new CreateUserHandler(repository);

        var result = await handler.HandleAsync(
            new CreateUserCommand("alice", "Alice"),
            CancellationToken.None);

        Assert.Equal("alice", result.Username);
        Assert.Equal("Alice", result.NickName);
        Assert.NotEqual(Guid.Empty, result.UserId.Value);
        Assert.Equal(1, repository.TryAddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        var repository = new FakeUserRepository();
        var handler = new CreateUserHandler(repository);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await handler.HandleAsync(
            new CreateUserCommand("alice", "Alice"),
            cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(act);
    }

    // -------------------------------------------------------------------------
    // Fake
    // -------------------------------------------------------------------------

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly bool _tryAddResult;

        public FakeUserRepository(bool tryAddResult = true)
        {
            _tryAddResult = tryAddResult;
        }

        public int TryAddCallCount { get; private set; }

        public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryAddCallCount++;
            return Task.FromResult(_tryAddResult);
        }

        public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not used by CreateUserHandler.");

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not used by CreateUserHandler.");

        public Task<bool> TrySaveAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not used by CreateUserHandler.");

        public Task<bool> TryDeleteAsync(UserId userId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not used by CreateUserHandler.");
    }
}

