using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RiasBot.Migrations
{
    public partial class MuteTimers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MuteUntil",
                table: "UserGuilds",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "MuteTimers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    Moderator = table.Column<ulong>(nullable: false),
                    MuteChannelSource = table.Column<ulong>(nullable: false),
                    MutedUntil = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuteTimers", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MuteTimers");

            migrationBuilder.DropColumn(
                name: "MuteUntil",
                table: "UserGuilds");
        }
    }
}
