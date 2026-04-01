# Test Quick Reference

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/BOTC.Domain.Tests/

# Run with verbose output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter "ClassName=RoomPlayerTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~RoomPlayerTests.Create_WhenInputIsValid"
```

## Test Statistics

- **Total Tests**: 92
- **Domain Tests**: 53 (test coverage for Room, RoomPlayer, RoomId, RoomCode)
- **Application Tests**: 33 (test coverage for use case handlers with fakes)
- **Architecture Tests**: 6 (dependency direction and clean architecture validation)

## Test Organization

```
tests/
├── BOTC.Architecture.Tests/
│   └── UnitTest1.cs (6 tests)
│       - Dependency direction validation
│       - Framework independence checks
│
├── BOTC.Domain.Tests/
│   ├── UnitTest1.cs (1 test - RoomIdTests)
│   ├── Rooms/
│   │   ├── RoomCodeTests.cs (7 tests)
│   │   ├── RoomTests.cs (25 tests)
│   │   └── RoomPlayerTests.cs (20 tests)
│
├── BOTC.Application.Tests/
│   ├── UnitTest1.cs (3 tests - contract tests)
│   └── Features/Rooms/
│       ├── CreateRoom/CreateRoomHandlerTests.cs (7 tests)
│       ├── JoinRoom/JoinRoomHandlerTests.cs (7 tests)
│       ├── LeaveRoom/LeaveRoomHandlerTests.cs (7 tests)
│       ├── SetPlayerReady/SetPlayerReadyHandlerTests.cs (1 test)
│       └── StartGame/StartGameHandlerTests.cs (1 test)
│
├── BOTC.Presentation.Api.Tests/
│   └── JoinRoomEndpointTests.cs (8 tests)
│
└── BOTC.Infrastructure.Tests/
    └── Rooms/
        ├── RoomRepositoryTests.cs (10 tests)
        ├── RoomLobbyReadRepositoryTests.cs (existing)
        └── RandomRoomCodeGeneratorTests.cs (existing)
```

## Key Test Patterns Used

### 1. Behavior-Based Testing
Tests verify what the code *does*, not how it does it.

```csharp
[Fact]
public void Create_WhenInputIsValid_CreatesPlayerWithExpectedProperties()
{
    var player = RoomPlayer.Create(id, "TestPlayer", RoomPlayerRole.Player, now);
    Assert.Equal("TestPlayer", player.DisplayName);
}
```

### 2. Fake Dependencies
Simple, focused test doubles instead of mocks.

```csharp
private sealed class FakeJoinRoomHandler
{
    public Task<JoinRoomResult> HandleAsync(JoinRoomCommand command, CancellationToken ct)
    {
        // Simple, readable test double
    }
}
```

### 3. Arrange-Act-Assert
Clear separation of test phases.

```csharp
// Arrange
var room = CreateTestRoom();

// Act
var result = room.JoinPlayer("Alice", DateTime.UtcNow);

// Assert
Assert.Equal("Alice", result.DisplayName);
```

### 4. Theory Tests with InlineData
Data-driven tests for validation scenarios.

```csharp
[Theory]
[InlineData("")]
[InlineData("   ")]
public void Create_WhenDisplayNameIsEmptyOrWhitespace_Throws(string name)
{
    Assert.Throws<ArgumentException>(() => RoomPlayer.Create(id, name, role, now));
}
```

## Domains Covered

### Domain Layer
- ✅ RoomId (value object)
- ✅ RoomCode (value object)
- ✅ RoomPlayerId (value object)
- ✅ RoomPlayer (value object, immutability)
- ✅ Room (aggregate root, invariants)
- ✅ Game logic (join, leave, ready, start)

### Application Layer
- ✅ CreateRoom handler with retry logic
- ✅ JoinRoom handler with conflict detection
- ✅ LeaveRoom handler with host transfer
- ✅ SetPlayerReady handler
- ✅ StartGame handler with validation
- ✅ GetRoomLobby handler

### Presentation Layer
- ✅ JoinRoom endpoint (validation, error handling, notifications)

### Architecture
- ✅ Dependency direction enforcement
- ✅ Framework independence validation
- ✅ Clean Architecture boundaries

## Common Test Utilities

### Test Room Creation
```csharp
private static Room CreateTestRoom()
{
    return Room.Create(
        RoomId.New(),
        new RoomCode("TEST01"),
        "TestHost",
        DateTime.UtcNow);
}
```

### Accessing Room Properties
```csharp
private static RoomPlayerId GetHostId(Room room)
{
    return room.Players.Single(p => p.Role == RoomPlayerRole.Host).Id;
}
```

## Maintenance Tips

1. **Keep tests focused** - One behavior per test
2. **Use clear names** - Name explains scenario and expected outcome
3. **Arrange test data** - Use helper methods instead of inline setup
4. **Assert explicitly** - Use specific assertions, not generic ones
5. **Avoid mocks** - Use simple fakes and test doubles
6. **No implementation details** - Test behavior, not private methods
7. **Isolate tests** - Tests should not depend on execution order

## CI/CD Integration

Tests run automatically and are expected to pass:
```bash
# Pre-commit hook would run
dotnet test --no-build

# Build pipeline would run
dotnet build
dotnet test --logger "trx"
```

## Adding New Tests

When implementing new features:

1. Write test first (TDD) or alongside implementation
2. Use existing patterns (Arrange-Act-Assert, Theory with InlineData)
3. Place test in correct project layer
4. Use meaningful names describing behavior
5. Run full test suite to verify no regressions: `dotnet test`

