using Microsoft.EntityFrameworkCore.Migrations;

namespace Rias.Migrations
{
    public partial class SarNameRemoved : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role_name",
                table: "self_assignable_roles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role_name",
                table: "self_assignable_roles",
                type: "text",
                nullable: true);
        }
    }
}
