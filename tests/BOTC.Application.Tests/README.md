# BOTC.Application.Tests

## Create Room v1 tests

This test set validates `CreateRoomHandler` behavior in the Application layer (use-case orchestration), including success paths, retry logic, and failure handling.

### What is covered

- Constructor guard clauses (`IRoomRepository`, `IRoomCodeGenerator` must not be null)
- `HandleAsync` guard clause (`CreateRoomCommand` must not be null)
- Successful room creation on first unique code
- Retry behavior when first generated code collides
- Exhausted retries after repeated collisions (`RoomCodeGenerationExhaustedException`)
- Fast failure when generator returns an invalid room code
- Exception propagation when repository fails
- Cancellation handling before work starts

### Fake strategy

Tests use focused in-memory fakes instead of mocks:

- `FakeRoomRepository`
  - Stores existing room codes in a `HashSet<RoomCode>`
  - Implements `TryAddAsync(Room, CancellationToken)` using a single `HashSet.Add` operation to simulate an atomic "add if unique" contract
  - Supports seeding collisions and injecting a repository exception
  - Exposes call count for explicit behavioral assertions

- `FakeRoomCodeGenerator`
  - Returns predefined codes from a queue
  - Makes retry scenarios deterministic
  - Exposes generate call count for explicit assertions

### Scenario list

1. Constructor throws when repository is null
2. Constructor throws when room code generator is null
3. `HandleAsync` throws when command is null
4. First generated code is unique -> room is created
5. First code collides, second is unique -> retries and succeeds
6. All attempts collide -> throws exhausted generation exception
7. Generator returns invalid code -> fails before repository call
8. Repository throws -> exception is propagated
9. Cancellation requested -> throws `OperationCanceledException` without side effects

## Run tests

```powershell
dotnet test .\tests\BOTC.Application.Tests\BOTC.Application.Tests.csproj
```

Optional: run all tests in the solution.

```powershell
dotnet test .\BOTC.slnx
```

