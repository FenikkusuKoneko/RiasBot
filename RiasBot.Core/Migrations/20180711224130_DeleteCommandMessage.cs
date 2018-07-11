using Microsoft.EntityFrameworkCore.Migrations;

namespace RiasBot.Migrations
{
    public partial class DeleteCommandMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeleteCommandMessage",
                table: "Guilds",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteCommandMessage",
                table: "Guilds");
        }
    }
}
