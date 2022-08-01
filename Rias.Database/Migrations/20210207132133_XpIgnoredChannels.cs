using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rias.Migrations
{
    public partial class XpIgnoreChannels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<ulong>>(
                name: "xp_ignored_channels",
                table: "guilds",
                type: "numeric(20,0)[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xp_ignored_channels",
                table: "guilds");
        }
    }
}
