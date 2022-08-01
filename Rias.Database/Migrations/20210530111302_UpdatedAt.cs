using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rias.Migrations
{
    public partial class UpdatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "warnings",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "waifus",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "votes",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "self_assignable_roles",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "profile",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "patreon",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "mute_timers",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "members",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "guilds",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "guild_xp_roles",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "custom_waifus",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "custom_characters",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "characters",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "warnings");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "waifus");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "votes");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "self_assignable_roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "patreon");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "mute_timers");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "members");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "guild_xp_roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "custom_waifus");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "custom_characters");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "characters");
        }
    }
}
