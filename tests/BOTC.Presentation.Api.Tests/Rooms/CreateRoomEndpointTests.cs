﻿using System.Reflection;
using System.Text;
using System.Text.Json;
using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Abstractions.Services;
using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Rooms;
using BOTC.Presentation.Api.Rooms;
using BOTC.Presentation.Api.Rooms.Realtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Api.Tests.Rooms;

public sealed class CreateRoomEndpointTests
{
    [Fact]
    public async Task CreateRoomAsync_WhenRequestIsValid_Returns201WithExpectedResponsePayload()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var codeGenerator = new FakeRoomCodeGenerator(["AB12CD"]);
        var handler = new CreateRoomHandler(repository, codeGenerator);
        var notifier = new FakeRoomLobbyNotifier();
        var request = new CreateRoomRequest("Host");

        // Act
        var result = await InvokeCreateRoomAsync(request, handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
        Assert.StartsWith("/api/rooms/", response.Location ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal(["AB12CD"], notifier.NotifiedRoomCodes);

        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal(repository.LastAddedRoom!.Id.Value.ToString(), root.GetProperty("roomId").GetString());
        Assert.Equal("AB12CD", root.GetProperty("roomCode").GetString());
        Assert.Equal(repository.LastAddedRoom.HostPlayerId.Value.ToString(), root.GetProperty("playerId").GetString());
        Assert.Equal(
            repository.LastAddedRoom.CreatedAtUtc,
            root.GetProperty("createdAtUtc").GetDateTime());
    }

    [Fact]
    public async Task CreateRoomAsync_WhenHostDisplayNameIsWhitespace_Returns400ProblemDetails()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var codeGenerator = new FakeRoomCodeGenerator(["AB12CD"]);
        var handler = new CreateRoomHandler(repository, codeGenerator);
        var notifier = new FakeRoomLobbyNotifier();
        var request = new CreateRoomRequest("   ");

        // Act
        var result = await InvokeCreateRoomAsync(request, handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal("Invalid create room request.", root.GetProperty("title").GetString());
        Assert.Equal("HostDisplayName is required.", root.GetProperty("detail").GetString());
        Assert.Equal(StatusCodes.Status400BadRequest, root.GetProperty("status").GetInt32());
        Assert.Equal(0, repository.TryAddCallCount);
        Assert.Empty(notifier.NotifiedRoomCodes);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenCodeGenerationIsExhausted_Returns503ProblemDetails()
    {
        // Arrange
        var repository = new FakeRoomRepository(alwaysCollide: true);
        var codeGenerator = new FakeRoomCodeGenerator(Enumerable.Repeat("AB12CD", 10));
        var handler = new CreateRoomHandler(repository, codeGenerator);
        var notifier = new FakeRoomLobbyNotifier();
        var request = new CreateRoomRequest("Host");

        // Act
        var result = await InvokeCreateRoomAsync(request, handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
        Assert.Empty(notifier.NotifiedRoomCodes);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal("Room code generation temporarily unavailable.", root.GetProperty("title").GetString());
        Assert.Contains("10", root.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenHostDisplayNameHasSpaces_RequestValueFlowsThroughDomainNormalization()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var codeGenerator = new FakeRoomCodeGenerator(["ZX98QP"]);
        var handler = new CreateRoomHandler(repository, codeGenerator);
        var notifier = new FakeRoomLobbyNotifier();
        var request = new CreateRoomRequest("  Alice  ");

        // Act
        var result = await InvokeCreateRoomAsync(request, handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
        Assert.NotNull(repository.LastAddedRoom);
        Assert.Equal("Alice", repository.LastAddedRoom!.HostDisplayName);
        Assert.Equal("ZX98QP", repository.LastAddedRoom.Code.Value);
        Assert.Equal(["ZX98QP"], notifier.NotifiedRoomCodes);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenGeneratedCodeIsInvalid_Returns400ProblemDetails()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var codeGenerator = new FakeRoomCodeGenerator(["abc"]);
        var handler = new CreateRoomHandler(repository, codeGenerator);
        var notifier = new FakeRoomLobbyNotifier();
        var request = new CreateRoomRequest("Host");

        // Act
        var result = await InvokeCreateRoomAsync(request, handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal("Invalid create room request.", root.GetProperty("title").GetString());
        Assert.Contains("Room code", root.GetProperty("detail").GetString(), StringComparison.Ordinal);
        Assert.Equal(0, repository.TryAddCallCount);
        Assert.Empty(notifier.NotifiedRoomCodes);
    }

    private static async Task<IResult> InvokeCreateRoomAsync(
        CreateRoomRequest request,
        CreateRoomHandler handler,
        IRoomLobbyNotifier notifier,
        CancellationToken cancellationToken)
    {
        var method = typeof(CreateRoomEndpoints).GetMethod(
            "CreateRoomAsync",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(CreateRoomRequest), typeof(CreateRoomHandler), typeof(IRoomLobbyNotifier), typeof(CancellationToken)],
            null);

        Assert.NotNull(method);

        var invocationResult = method!.Invoke(null, [request, handler, notifier, cancellationToken]);
        Assert.NotNull(invocationResult);

        return await (Task<IResult>)invocationResult!;
    }

    private static async Task<HttpExecutionResult> ExecuteResultAsync(IResult result)
    {
        var httpContext = new DefaultHttpContext();
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        httpContext.RequestServices = serviceProvider;

        await using var bodyStream = new MemoryStream();
        httpContext.Response.Body = bodyStream;

        await result.ExecuteAsync(httpContext);

        bodyStream.Position = 0;
        using var reader = new StreamReader(bodyStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        await serviceProvider.DisposeAsync();

        return new HttpExecutionResult(
            httpContext.Response.StatusCode,
            body,
            httpContext.Response.Headers.Location.ToString());
    }

    private sealed record HttpExecutionResult(int StatusCode, string Body, string? Location);

    private sealed class FakeRoomRepository : IRoomRepository
    {
        private readonly bool alwaysCollide;

        public FakeRoomRepository(bool alwaysCollide = false)
        {
            this.alwaysCollide = alwaysCollide;
        }

        public Room? LastAddedRoom { get; private set; }

        public int TryAddCallCount { get; private set; }

        public Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryAddCallCount++;

            if (alwaysCollide)
            {
                return Task.FromResult(false);
            }

            LastAddedRoom = room;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeRoomCodeGenerator : IRoomCodeGenerator
    {
        private readonly Queue<string> codes;

        public FakeRoomCodeGenerator(IEnumerable<string> codes)
        {
            this.codes = new Queue<string>(codes);
        }

        public string Generate()
        {
            if (codes.Count == 0)
            {
                throw new InvalidOperationException("No more codes configured for test.");
            }

            return codes.Dequeue();
        }
    }

    private sealed class FakeRoomLobbyNotifier : IRoomLobbyNotifier
    {
        public List<string> NotifiedRoomCodes { get; } = [];

        public Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken)
        {
            NotifiedRoomCodes.Add(roomCode);
            return Task.CompletedTask;
        }

        public Task NotifyLobbyClosedAsync(string roomCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
