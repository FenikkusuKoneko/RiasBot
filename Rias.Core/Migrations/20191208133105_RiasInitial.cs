using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Rias.Core.Migrations
{
    public partial class RiasInitial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    character_id = table.Column<int>(),
                    name = table.Column<string>(nullable: true),
                    url = table.Column<string>(nullable: true),
                    image_url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters", x => x.id);
                    table.UniqueConstraint("AK_characters_character_id", x => x.character_id);
                });

            migrationBuilder.CreateTable(
                name: "custom_characters",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    character_id = table.Column<int>(),
                    name = table.Column<string>(nullable: true),
                    image_url = table.Column<string>(nullable: true),
                    description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_characters", x => x.id);
                    table.UniqueConstraint("AK_custom_characters_character_id", x => x.character_id);
                });

            migrationBuilder.CreateTable(
                name: "custom_waifus",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    user_id = table.Column<decimal>(),
                    name = table.Column<string>(nullable: true),
                    image_url = table.Column<string>(nullable: true),
                    is_special = table.Column<bool>(),
                    position = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_waifus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guild_users",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    guild_id = table.Column<decimal>(),
                    user_id = table.Column<decimal>(),
                    is_muted = table.Column<bool>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guild_xp_roles",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    guild_id = table.Column<decimal>(),
                    level = table.Column<int>(),
                    role_id = table.Column<decimal>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_xp_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guilds",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    guild_id = table.Column<decimal>(),
                    prefix = table.Column<string>(nullable: true),
                    mute_role_id = table.Column<decimal>(),
                    greet_notification = table.Column<bool>(),
                    greet_message = table.Column<string>(nullable: true),
                    greet_webhook_id = table.Column<decimal>(),
                    bye_notification = table.Column<bool>(),
                    bye_message = table.Column<string>(nullable: true),
                    bye_webhook_id = table.Column<decimal>(),
                    guild_xp_notification = table.Column<bool>(),
                    auto_assignable_role_id = table.Column<decimal>(),
                    punishment_warnings_required = table.Column<int>(),
                    warning_punishment = table.Column<string>(nullable: true),
                    mod_log_channel_id = table.Column<decimal>(),
                    delete_command_message = table.Column<bool>(),
                    locale = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guilds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guilds_xp",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    guild_id = table.Column<decimal>(),
                    user_id = table.Column<decimal>(),
                    xp = table.Column<int>(),
                    last_message_date = table.Column<DateTime>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guilds_xp", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mute_timers",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    guild_id = table.Column<decimal>(),
                    user_id = table.Column<decimal>(),
                    moderator_id = table.Column<decimal>(),
                    mute_channel_source_id = table.Column<decimal>(),
                    expiration = table.Column<DateTime>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mute_timers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    user_id = table.Column<decimal>(),
                    background_url = table.Column<string>(nullable: true),
                    background_dim = table.Column<int>(),
                    biography = table.Column<string>(nullable: true),
                    color = table.Column<string>(nullable: true),
                    badges = table.Column<string[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "self_assignable_roles",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    guild_id = table.Column<decimal>(),
                    role_name = table.Column<string>(nullable: true),
                    role_id = table.Column<decimal>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_self_assignable_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    user_id = table.Column<decimal>(),
                    currency = table.Column<int>(),
                    xp = table.Column<int>(),
                    last_message_date = table.Column<DateTime>(),
                    is_blacklisted = table.Column<bool>(),
                    is_banned = table.Column<bool>(),
                    daily_taken = table.Column<DateTime>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "warnings",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    guild_id = table.Column<decimal>(),
                    user_id = table.Column<decimal>(),
                    reason = table.Column<string>(nullable: true),
                    moderator_id = table.Column<decimal>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warnings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "waifus",
                columns: table => new
                {
                    id = table.Column<int>()
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTime>(nullable: true),
                    user_id = table.Column<decimal>(),
                    character_id = table.Column<int>(nullable: true),
                    custom_character_id = table.Column<int>(nullable: true),
                    custom_image_url = table.Column<string>(nullable: true),
                    price = table.Column<int>(),
                    is_special = table.Column<bool>(),
                    position = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waifus", x => x.id);
                    table.ForeignKey(
                        name: "FK_waifus_characters_character_id",
                        column: x => x.character_id,
                        principalTable: "characters",
                        principalColumn: "character_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_waifus_custom_characters_custom_character_id",
                        column: x => x.custom_character_id,
                        principalTable: "custom_characters",
                        principalColumn: "character_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_characters_character_id",
                table: "characters",
                column: "character_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_custom_characters_character_id",
                table: "custom_characters",
                column: "character_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guilds_guild_id",
                table: "guilds",
                column: "guild_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_user_id",
                table: "profile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_user_id",
                table: "users",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_waifus_character_id",
                table: "waifus",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "IX_waifus_custom_character_id",
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
                name: "guilds_xp");

            migrationBuilder.DropTable(
                name: "mute_timers");

            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "self_assignable_roles");

            migrationBuilder.DropTable(
                name: "users");

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
