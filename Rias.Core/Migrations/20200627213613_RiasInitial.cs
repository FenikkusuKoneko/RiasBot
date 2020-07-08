using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Rias.Core.Commons;

namespace Rias.Core.Migrations
{
    public partial class RiasInitial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:last_charge_status", "paid,declined,pending,refunded,fraud,other")
                .Annotation("Npgsql:Enum:patron_status", "active_patron,declined_patron,former_patron");

            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    character_id = table.Column<int>(nullable: false),
                    name = table.Column<string>(nullable: true),
                    url = table.Column<string>(nullable: true),
                    image_url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_characters", x => x.id);
                    table.UniqueConstraint("AK_characters_character_id", x => x.character_id);
                });

            migrationBuilder.CreateTable(
                name: "custom_characters",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    character_id = table.Column<int>(nullable: false),
                    name = table.Column<string>(nullable: true),
                    image_url = table.Column<string>(nullable: true),
                    description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_characters", x => x.id);
                    table.UniqueConstraint("AK_custom_characters_character_id", x => x.character_id);
                });

            migrationBuilder.CreateTable(
                name: "custom_waifus",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<decimal>(nullable: false),
                    name = table.Column<string>(nullable: true),
                    image_url = table.Column<string>(nullable: true),
                    is_special = table.Column<bool>(nullable: false),
                    position = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_waifus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guild_users",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    guild_id = table.Column<decimal>(nullable: false),
                    user_id = table.Column<decimal>(nullable: false),
                    xp = table.Column<int>(nullable: false),
                    last_message_date = table.Column<DateTime>(nullable: false),
                    is_muted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guild_xp_roles",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    guild_id = table.Column<decimal>(nullable: false),
                    role_id = table.Column<decimal>(nullable: false),
                    level = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_xp_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guilds",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    guild_id = table.Column<decimal>(nullable: false),
                    prefix = table.Column<string>(nullable: true),
                    mute_role_id = table.Column<decimal>(nullable: false),
                    greet_notification = table.Column<bool>(nullable: false),
                    greet_message = table.Column<string>(nullable: true),
                    greet_webhook_id = table.Column<decimal>(nullable: false),
                    bye_notification = table.Column<bool>(nullable: false),
                    bye_message = table.Column<string>(nullable: true),
                    bye_webhook_id = table.Column<decimal>(nullable: false),
                    guild_xp_notification = table.Column<bool>(nullable: false),
                    auto_assignable_role_id = table.Column<decimal>(nullable: false),
                    punishment_warnings_required = table.Column<int>(nullable: false),
                    warning_punishment = table.Column<string>(nullable: true),
                    mod_log_channel_id = table.Column<decimal>(nullable: false),
                    delete_command_message = table.Column<bool>(nullable: false),
                    locale = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guilds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mute_timers",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    guild_id = table.Column<decimal>(nullable: false),
                    user_id = table.Column<decimal>(nullable: false),
                    moderator_id = table.Column<decimal>(nullable: false),
                    mute_channel_source_id = table.Column<decimal>(nullable: false),
                    expiration = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mute_timers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patreon",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<decimal>(nullable: false),
                    patreon_user_id = table.Column<int>(nullable: false),
                    patreon_user_name = table.Column<string>(nullable: true),
                    amount_cents = table.Column<int>(nullable: false),
                    last_charge_date = table.Column<DateTimeOffset>(nullable: true),
                    last_charge_status = table.Column<LastChargeStatus>(nullable: true),
                    patron_status = table.Column<PatronStatus>(nullable: true),
                    tier = table.Column<int>(nullable: false),
                    tier_amount_cents = table.Column<int>(nullable: false),
                    @checked = table.Column<bool>(name: "checked", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patreon", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<decimal>(nullable: false),
                    background_url = table.Column<string>(nullable: true),
                    background_dim = table.Column<int>(nullable: false),
                    biography = table.Column<string>(nullable: true),
                    color = table.Column<string>(nullable: true),
                    badges = table.Column<string[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "self_assignable_roles",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    guild_id = table.Column<decimal>(nullable: false),
                    role_id = table.Column<decimal>(nullable: false),
                    role_name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_self_assignable_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<decimal>(nullable: false),
                    currency = table.Column<int>(nullable: false),
                    xp = table.Column<int>(nullable: false),
                    last_message_date = table.Column<DateTime>(nullable: false),
                    is_blacklisted = table.Column<bool>(nullable: false),
                    is_banned = table.Column<bool>(nullable: false),
                    daily_taken = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<decimal>(nullable: false),
                    type = table.Column<string>(nullable: true),
                    query = table.Column<string>(nullable: true),
                    is_weekend = table.Column<bool>(nullable: false),
                    @checked = table.Column<bool>(name: "checked", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_votes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "warnings",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    guild_id = table.Column<decimal>(nullable: false),
                    user_id = table.Column<decimal>(nullable: false),
                    reason = table.Column<string>(nullable: true),
                    moderator_id = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warnings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "waifus",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<decimal>(nullable: false),
                    character_id = table.Column<int>(nullable: true),
                    custom_character_id = table.Column<int>(nullable: true),
                    custom_image_url = table.Column<string>(nullable: true),
                    price = table.Column<int>(nullable: false),
                    is_special = table.Column<bool>(nullable: false),
                    position = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waifus", x => x.id);
                    table.ForeignKey(
                        name: "fk_waifus_characters_character_id",
                        column: x => x.character_id,
                        principalTable: "characters",
                        principalColumn: "character_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waifus_custom_characters_custom_character_id",
                        column: x => x.custom_character_id,
                        principalTable: "custom_characters",
                        principalColumn: "character_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_characters_character_id",
                table: "characters",
                column: "character_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_custom_characters_character_id",
                table: "custom_characters",
                column: "character_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guild_users_guild_id_user_id",
                table: "guild_users",
                columns: new[] { "guild_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guild_xp_roles_guild_id_role_id",
                table: "guild_xp_roles",
                columns: new[] { "guild_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guilds_guild_id",
                table: "guilds",
                column: "guild_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mute_timers_guild_id_user_id",
                table: "mute_timers",
                columns: new[] { "guild_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_profile_user_id",
                table: "profile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_self_assignable_roles_guild_id_role_id",
                table: "self_assignable_roles",
                columns: new[] { "guild_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_user_id",
                table: "users",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waifus_character_id",
                table: "waifus",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "ix_waifus_custom_character_id",
                table: "waifus",
                column: "custom_character_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_waifus");

            migrationBuilder.DropTable(
                name: "guild_users");

            migrationBuilder.DropTable(
                name: "guild_xp_roles");

            migrationBuilder.DropTable(
                name: "guilds");

            migrationBuilder.DropTable(
                name: "mute_timers");

            migrationBuilder.DropTable(
                name: "patreon");

            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "self_assignable_roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "votes");

            migrationBuilder.DropTable(
                name: "waifus");

            migrationBuilder.DropTable(
                name: "warnings");

            migrationBuilder.DropTable(
                name: "characters");

            migrationBuilder.DropTable(
                name: "custom_characters");
        }
    }
}
