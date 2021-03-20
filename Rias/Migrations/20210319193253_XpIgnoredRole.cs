using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rias.Migrations
{
    public partial class XpIgnoredRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_xp_ignored",
                table: "members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal[]>(
                name: "xp_ignored_channels",
                table: "guilds",
                type: "numeric[]",
                nullable: true,
                oldClrType: typeof(decimal[]),
                oldType: "numeric(20,0)[]",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "xp_ignored_role_id",
                table: "guilds",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_xp_ignored",
                table: "members");

            migrationBuilder.DropColumn(
                name: "xp_ignored_role_id",
                table: "guilds");

            migrationBuilder.AlterColumn<decimal[]>(
                name: "xp_ignored_channels",
                table: "guilds",
                type: "numeric(20,0)[]",
                nullable: true,
                oldClrType: typeof(decimal[]),
                oldType: "numeric[]",
                oldNullable: true);
        }
    }
}
