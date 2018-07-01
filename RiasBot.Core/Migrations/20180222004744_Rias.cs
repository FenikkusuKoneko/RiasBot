using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RiasBot.Migrations
{
    public partial class Rias : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Bye = table.Column<bool>(nullable: false),
                    ByeChannel = table.Column<ulong>(nullable: false),
                    ByeMessage = table.Column<string>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Greet = table.Column<bool>(nullable: false),
                    GreetChannel = table.Column<ulong>(nullable: false),
                    GreetMessage = table.Column<string>(nullable: true),
                    GuildId = table.Column<ulong>(nullable: false),
                    MuteRole = table.Column<ulong>(nullable: false),
                    Prefix = table.Column<string>(nullable: true),
                    XpGuildNotification = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patreon",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    NextTimeReward = table.Column<DateTime>(nullable: false),
                    Reward = table.Column<int>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patreon", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Currency = table.Column<int>(nullable: false),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Level = table.Column<int>(nullable: false),
                    MessageDateTime = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    Xp = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Waifus",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    IsPrimary = table.Column<bool>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    WaifuId = table.Column<int>(nullable: false),
                    WaifuName = table.Column<string>(nullable: true),
                    WaifuPicture = table.Column<string>(nullable: true),
                    WaifuPrice = table.Column<int>(nullable: false),
                    WaifuUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waifus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warnings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Moderator = table.Column<ulong>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    UserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warnings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XpSystem",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    MessageDateTime = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    Xp = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpSystem", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_GuildId",
                table: "Guilds",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patreon_UserId",
                table: "Patreon",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserId",
                table: "Users",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "Patreon");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Waifus");

            migrationBuilder.DropTable(
                name: "Warnings");

            migrationBuilder.DropTable(
                name: "XpSystem");
        }
    }
}
