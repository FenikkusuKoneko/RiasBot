using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RiasBot.Migrations
{
    public partial class WarningsPunishment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PunishmentMethod",
                table: "Guilds",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarnsPunishment",
                table: "Guilds",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PunishmentMethod",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "WarnsPunishment",
                table: "Guilds");
        }
    }
}
