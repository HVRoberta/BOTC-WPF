﻿using System.Reflection;
using System.Text;
using System.Text.Json;
using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Domain.Rooms;
using BOTC.Presentation.Api.Rooms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Api.Tests.Rooms;

public sealed class GetRoomLobbyEndpointTests
{
    [Fact]
    public async Task GetRoomLobbyAsync_WhenRoomExists_Returns200WithLobbyPayload()
    {
        // Arrange
        var repository = new FakeRoomLobbyQueryService();
        repository.SeedLobby(new GetRoomLobbyResult(
            new RoomCode("AB12CD"),
            [
                new LobbyPlayerResult(new RoomPlayerId(Guid.Parse("11111111-1111-1111-1111-111111111111")), "Host", RoomPlayerRole.Host),
                new LobbyPlayerResult(new RoomPlayerId(Guid.Parse("22222222-2222-2222-2222-222222222222")), "Alice", RoomPlayerRole.Player)
            ],
            RoomStatus.WaitingForPlayers));

        var handler = new GetRoomLobbyHandler(repository);

        // Act
        var result = await InvokeGetRoomLobbyAsync("AB12CD", handler, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);

        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;

        Assert.Equal("AB12CD", root.GetProperty("roomCode").GetString());
        Assert.Equal((int)RoomStatus.WaitingForPlayers, root.GetProperty("status").GetInt32());

        var players = root.GetProperty("players").EnumerateArray().ToArray();
        Assert.Equal(2, players.Length);
        Assert.Equal("Host", players[0].GetProperty("displayName").GetString());
        Assert.True(players[0].GetProperty("isHost").GetBoolean());
    }

    [Fact]
    public async Task GetRoomLobbyAsync_WhenRoomCodeIsInvalid_Returns400ProblemDetails()
    {
        // Arrange
        var handler = new GetRoomLobbyHandler(new FakeRoomLobbyQueryService());

        // Act
        var result = await InvokeGetRoomLobbyAsync("abc", handler, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal("Invalid room code.", root.GetProperty("title").GetString());
        Assert.Equal(StatusCodes.Status400BadRequest, root.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetRoomLobbyAsync_WhenRoomDoesNotExist_Returns404ProblemDetails()
    {
        // Arrange
        var handler = new GetRoomLobbyHandler(new FakeRoomLobbyQueryService());

        // Act
        var result = await InvokeGetRoomLobbyAsync("AB12CD", handler, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal("Room not found.", root.GetProperty("title").GetString());
        Assert.Equal(StatusCodes.Status404NotFound, root.GetProperty("status").GetInt32());
    }

    private static async Task<IResult> InvokeGetRoomLobbyAsync(
        string roomCode,
        GetRoomLobbyHandler handler,
        CancellationToken cancellationToken)
    {
        var method = typeof(RoomsEndpoints).GetMethod(
            "GetRoomLobbyAsync",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(GetRoomLobbyHandler), typeof(CancellationToken)],
            null);

        Assert.NotNull(method);

        var invocationResult = method!.Invoke(null, [roomCode, handler, cancellationToken]);
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

        return new HttpExecutionResult(httpContext.Response.StatusCode, body);
    }

    private sealed record HttpExecutionResult(int StatusCode, string Body);

    private sealed class FakeRoomLobbyQueryService : IRoomLobbyQueryService
    {
        private readonly Dictionary<string, GetRoomLobbyResult> lobbiesByCode = new(StringComparer.Ordinal);

        public Task<GetRoomLobbyResult?> GetByRoomCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lobbiesByCode.TryGetValue(roomCode.Value, out var result);
            return Task.FromResult(result);
        }

        public void SeedLobby(GetRoomLobbyResult lobby)
        {
            lobbiesByCode[lobby.RoomCode.Value] = lobby;
        }
    }
}
