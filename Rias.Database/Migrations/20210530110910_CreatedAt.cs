using Microsoft.EntityFrameworkCore.Migrations;

namespace Rias.Migrations
{
    public partial class CreatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "warnings",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "waifus",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "votes",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "self_assignable_roles",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "profile",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "patreon",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "mute_timers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "members",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "guilds",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "guild_xp_roles",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "custom_waifus",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "custom_characters",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "date_added",
                table: "characters",
                newName: "created_at");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "warnings",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "waifus",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "votes",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "users",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "self_assignable_roles",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "profile",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "patreon",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "mute_timers",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "members",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "guilds",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "guild_xp_roles",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "custom_waifus",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "custom_characters",
                newName: "date_added");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "characters",
                newName: "date_added");
        }
    }
}
