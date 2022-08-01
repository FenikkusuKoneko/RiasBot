using Microsoft.EntityFrameworkCore.Migrations;

namespace Rias.Migrations
{
    public partial class XpChannel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "guild_xp_notification",
                table: "guilds",
                newName: "xp_notification");
            
            migrationBuilder.AddColumn<decimal>(
                name: "xp_webhook_id",
                table: "guilds",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "xp_level_up_message",
                table: "guilds",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "xp_level_up_role_reward_message",
                table: "guilds",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "xp_notification",
                table: "guilds",
                newName: "guild_xp_notification");
            
            migrationBuilder.DropColumn(
                name: "xp_webhook_id",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "xp_level_up_message",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "xp_level_up_role_reward_message",
                table: "guilds");
        }
    }
}
