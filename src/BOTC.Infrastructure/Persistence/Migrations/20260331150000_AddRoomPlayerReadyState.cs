using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOTC.Infrastructure.Persistence.Migrations
{
    public partial class AddRoomPlayerReadyState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReady",
                table: "RoomPlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReady",
                table: "RoomPlayers");
        }
    }
}

