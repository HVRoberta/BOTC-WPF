using BOTC.Application.Abstractions.Events;
using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Abstractions.Services;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.CreateRoom;

public sealed class CreateRoomHandler
{
    private const int MaxRoomCodeGenerationAttempts = 10;

    private readonly IRoomRepository _roomRepository;
    private readonly IRoomCodeGenerator _roomCodeGenerator;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public CreateRoomHandler(
        IRoomRepository roomRepository,
        IRoomCodeGenerator roomCodeGenerator,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _roomCodeGenerator = roomCodeGenerator ?? throw new ArgumentNullException(nameof(roomCodeGenerator));
        _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
    }

    public async Task<CreateRoomResult> HandleAsync(CreateRoomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        for (var attempt = 1; attempt <= MaxRoomCodeGenerationAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var generatedRoomCode = _roomCodeGenerator.Generate();
            var candidateCode = new RoomCode(generatedRoomCode);
            var room = Room.Create(
                RoomId.New(),
                candidateCode,
                command.HostDisplayName,
                DateTime.UtcNow);

            var added = await _roomRepository.TryAddAsync(room, cancellationToken);
            if (!added)
            {
                continue;
            }

            // Dispatch domain events after successful persistence.
            try
            {
                await _domainEventDispatcher.DispatchAsync(room.UncommittedEvents, cancellationToken);
            }
            finally
            {
                room.ClearUncommittedEvents();
            }

            return new CreateRoomResult(
                room.Id,
                room.Code,
                room.HostPlayerId,
                room.HostDisplayName,
                room.Status,
                room.CreatedAtUtc);
        }

        throw new RoomCodeGenerationExhaustedException(MaxRoomCodeGenerationAttempts);
    }
}
