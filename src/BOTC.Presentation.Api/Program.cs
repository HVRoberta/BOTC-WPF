using BOTC.Application;
using BOTC.Application.Abstractions.Realtime;
using BOTC.Contracts.Rooms;
using BOTC.Infrastructure;
using BOTC.Infrastructure.Persistence;
using BOTC.Presentation.Api.Rooms;
using BOTC.Presentation.Api.Rooms.Realtime;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string corsPolicyName = "ConfiguredCors";

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configuration value 'ConnectionStrings:Default' is required and cannot be empty.");
}

var clientBaseUrl = builder.Configuration["Client:BaseUrl"]
                    ?? throw new InvalidOperationException("Configuration value 'Client:BaseUrl' is required.");

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

var roomLobbyHubPath = builder.Configuration["SignalR:RoomLobbyHubPath"];
if (string.IsNullOrWhiteSpace(roomLobbyHubPath))
{
    roomLobbyHubPath = RoomLobbyHubContract.HubRoute;
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Wire the Application-layer IRoomLobbyNotifier to the Presentation SignalR implementation.
builder.Services.AddScoped<IRoomLobbyNotifier, SignalRRoomLobbyNotifier>();

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    app.Urls.Add($"http://*:{port}");
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BotcDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(corsPolicyName);

app.MapRoomsEndpoints();
app.MapHub<RoomLobbyHub>(roomLobbyHubPath);

app.Run();