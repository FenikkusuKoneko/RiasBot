using Microsoft.EntityFrameworkCore.Migrations;

namespace Rias.Migrations
{
    public partial class UserBlacklistRemoved : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_blacklisted",
                table: "users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_blacklisted",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
