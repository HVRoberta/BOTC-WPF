using BOTC.Application;
using BOTC.Contracts.Rooms;
using BOTC.Infrastructure;
using BOTC.Infrastructure.Persistence;
using BOTC.Presentation.Api.Rooms;
using BOTC.Presentation.Api.Rooms.Realtime;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=botc.db";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRoomLobbyNotifier, SignalRRoomLobbyNotifier>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BotcDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapRoomsEndpoints();
app.MapHub<RoomLobbyHub>(RoomLobbyHubContract.HubRoute);

app.Run();