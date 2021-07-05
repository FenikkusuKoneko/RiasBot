using Microsoft.EntityFrameworkCore.Migrations;

namespace Rias.Migrations
{
    public partial class PatreonWillPayAmountCents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "will_pay_amount_cents",
                table: "patreon",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "will_pay_amount_cents",
                table: "patreon");
        }
    }
}
