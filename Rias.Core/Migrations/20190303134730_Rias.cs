using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Rias.Core.Migrations
{
    public partial class Rias : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dailies",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    NextDaily = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dailies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    Prefix = table.Column<string>(nullable: true),
                    MuteRole = table.Column<decimal>(nullable: false),
                    Greet = table.Column<bool>(nullable: false),
                    GreetMessage = table.Column<string>(nullable: true),
                    GreetChannel = table.Column<decimal>(nullable: false),
                    Bye = table.Column<bool>(nullable: false),
                    ByeMessage = table.Column<string>(nullable: true),
                    ByeChannel = table.Column<decimal>(nullable: false),
                    XpGuildNotification = table.Column<bool>(nullable: false),
                    AutoAssignableRole = table.Column<decimal>(nullable: false),
                    WarnsPunishment = table.Column<int>(nullable: false),
                    PunishmentMethod = table.Column<string>(nullable: true),
                    ModLogChannel = table.Column<decimal>(nullable: false),
                    DeleteCommandMessage = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MuteTimers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    Moderator = table.Column<decimal>(nullable: false),
                    MuteChannelSource = table.Column<decimal>(nullable: false),
                    MutedUntil = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuteTimers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patreon",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    Reward = table.Column<int>(nullable: false),
                    NextTimeReward = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patreon", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profile",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    BackgroundUrl = table.Column<string>(nullable: true),
                    BackgroundDim = table.Column<int>(nullable: false),
                    MarriedUser = table.Column<decimal>(nullable: false),
                    Bio = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SelfAssignableRoles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    RoleName = table.Column<string>(nullable: true),
                    RoleId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfAssignableRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGuilds",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    IsMuted = table.Column<bool>(nullable: false),
                    MuteUntil = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGuilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    Currency = table.Column<int>(nullable: false),
                    Xp = table.Column<int>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    MessageDateTime = table.Column<DateTime>(nullable: false),
                    IsBlacklisted = table.Column<bool>(nullable: false),
                    IsBanned = table.Column<bool>(nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false),
                    WaifuId = table.Column<int>(nullable: false),
                    WaifuName = table.Column<string>(nullable: true),
                    WaifuUrl = table.Column<string>(nullable: true),
                    WaifuImage = table.Column<string>(nullable: true),
                    BelovedWaifuImage = table.Column<string>(nullable: true),
                    WaifuPrice = table.Column<int>(nullable: false),
                    IsPrimary = table.Column<bool>(nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    Moderator = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warnings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XpRolesSystem",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    RoleId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpRolesSystem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XpSystem",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    Xp = table.Column<int>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    MessageDateTime = table.Column<DateTime>(nullable: false)
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
                name: "IX_Profile_UserId",
                table: "Profile",
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
                name: "Dailies");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "MuteTimers");

            migrationBuilder.DropTable(
                name: "Patreon");

            migrationBuilder.DropTable(
                name: "Profile");

            migrationBuilder.DropTable(
                name: "SelfAssignableRoles");

            migrationBuilder.DropTable(
                name: "UserGuilds");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Waifus");

            migrationBuilder.DropTable(
                name: "Warnings");

            migrationBuilder.DropTable(
                name: "XpRolesSystem");

            migrationBuilder.DropTable(
                name: "XpSystem");
        }
    }
}
