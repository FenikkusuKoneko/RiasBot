using Microsoft.EntityFrameworkCore.Migrations;

namespace Rias.Migrations
{
    public partial class GuildUsersRefactoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "guild_users",
                newName: "member_id");

            migrationBuilder.RenameIndex(
                name: "ix_guild_users_guild_id_user_id",
                newName: "ix_members_guild_id_member_id",
                table: "guild_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_users",
                table: "guild_users");

            migrationBuilder.AddPrimaryKey(
                name: "pk_members",
                table: "guild_users",
                column: "id");
            
            migrationBuilder.RenameTable(
                name: "guild_users",
                newName: "members");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "member_id",
                table: "members",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_members_guild_id_member_id",
                newName: "ix_guild_users_guild_id_user_id",
                table: "members");

            migrationBuilder.DropPrimaryKey(
                name: "pk_members",
                table: "members");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_users",
                table: "members",
                column: "id");
            
            migrationBuilder.RenameTable(
                name: "members",
                newName: "guild_users");
        }
    }
}
