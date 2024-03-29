﻿// <auto-generated />

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Rias.Database;
using Rias.Database.Enums;

namespace Rias.Migrations
{
    [DbContext(typeof(RiasDbContext))]
    [Migration("20200627213613_RiasInitial")]
    partial class RiasInitial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:Enum:last_charge_status", "paid,declined,pending,refunded,fraud,other")
                .HasAnnotation("Npgsql:Enum:patron_status", "active_patron,declined_patron,former_patron")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Rias.Database.Entities.CharacterEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("CharacterId")
                        .HasColumnName("character_id")
                        .HasColumnType("integer");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ImageUrl")
                        .HasColumnName("image_url")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<string>("Url")
                        .HasColumnName("url")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_characters");

                    b.HasIndex("CharacterId")
                        .IsUnique()
                        .HasName("ix_characters_character_id");

                    b.ToTable("characters");
                });

            modelBuilder.Entity("Rias.Database.Entities.CustomCharacterEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("CharacterId")
                        .HasColumnName("character_id")
                        .HasColumnType("integer");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<string>("ImageUrl")
                        .HasColumnName("image_url")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_custom_characters");

                    b.HasIndex("CharacterId")
                        .IsUnique()
                        .HasName("ix_custom_characters_character_id");

                    b.ToTable("custom_characters");
                });

            modelBuilder.Entity("Rias.Database.Entities.CustomWaifuEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ImageUrl")
                        .HasColumnName("image_url")
                        .HasColumnType("text");

                    b.Property<bool>("IsSpecial")
                        .HasColumnName("is_special")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<int>("Position")
                        .HasColumnName("position")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id")
                        .HasName("pk_custom_waifus");

                    b.ToTable("custom_waifus");
                });

            modelBuilder.Entity("Rias.Database.Entities.GuildUserEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<bool>("IsMuted")
                        .HasColumnName("is_muted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastMessageDate")
                        .HasColumnName("last_message_date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Xp")
                        .HasColumnName("xp")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("pk_guild_users");

                    b.HasIndex("GuildId", "UserId")
                        .IsUnique()
                        .HasName("ix_guild_users_guild_id_user_id");

                    b.ToTable("guild_users");
                });

            modelBuilder.Entity("Rias.Database.Entities.GuildXpRoleEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Level")
                        .HasColumnName("level")
                        .HasColumnType("integer");

                    b.Property<decimal>("RoleId")
                        .HasColumnName("role_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id")
                        .HasName("pk_guild_xp_roles");

                    b.HasIndex("GuildId", "RoleId")
                        .IsUnique()
                        .HasName("ix_guild_xp_roles_guild_id_role_id");

                    b.ToTable("guild_xp_roles");
                });

            modelBuilder.Entity("Rias.Database.Entities.GuildEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<decimal>("AutoAssignableRoleId")
                        .HasColumnName("auto_assignable_role_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("ByeMessage")
                        .HasColumnName("bye_message")
                        .HasColumnType("text");

                    b.Property<bool>("ByeNotification")
                        .HasColumnName("bye_notification")
                        .HasColumnType("boolean");

                    b.Property<decimal>("ByeWebhookId")
                        .HasColumnName("bye_webhook_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("DeleteCommandMessage")
                        .HasColumnName("delete_command_message")
                        .HasColumnType("boolean");

                    b.Property<string>("GreetMessage")
                        .HasColumnName("greet_message")
                        .HasColumnType("text");

                    b.Property<bool>("GreetNotification")
                        .HasColumnName("greet_notification")
                        .HasColumnType("boolean");

                    b.Property<decimal>("GreetWebhookId")
                        .HasColumnName("greet_webhook_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<bool>("GuildXpNotification")
                        .HasColumnName("guild_xp_notification")
                        .HasColumnType("boolean");

                    b.Property<string>("Locale")
                        .HasColumnName("locale")
                        .HasColumnType("text");

                    b.Property<decimal>("ModLogChannelId")
                        .HasColumnName("mod_log_channel_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("MuteRoleId")
                        .HasColumnName("mute_role_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Prefix")
                        .HasColumnName("prefix")
                        .HasColumnType("text");

                    b.Property<int>("PunishmentWarningsRequired")
                        .HasColumnName("punishment_warnings_required")
                        .HasColumnType("integer");

                    b.Property<string>("WarningPunishment")
                        .HasColumnName("warning_punishment")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_guilds");

                    b.HasIndex("GuildId")
                        .IsUnique()
                        .HasName("ix_guilds_guild_id");

                    b.ToTable("guilds");
                });

            modelBuilder.Entity("Rias.Database.Entities.MuteTimerEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime>("Expiration")
                        .HasColumnName("expiration")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ModeratorId")
                        .HasColumnName("moderator_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("MuteChannelSourceId")
                        .HasColumnName("mute_channel_source_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id")
                        .HasName("pk_mute_timers");

                    b.HasIndex("GuildId", "UserId")
                        .IsUnique()
                        .HasName("ix_mute_timers_guild_id_user_id");

                    b.ToTable("mute_timers");
                });

            modelBuilder.Entity("Rias.Database.Entities.PatreonEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("AmountCents")
                        .HasColumnName("amount_cents")
                        .HasColumnType("integer");

                    b.Property<bool>("Checked")
                        .HasColumnName("checked")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTimeOffset?>("LastChargeDate")
                        .HasColumnName("last_charge_date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<LastChargeStatus?>("LastChargeStatus")
                        .HasColumnName("last_charge_status")
                        .HasColumnType("last_charge_status");

                    b.Property<int>("PatreonUserId")
                        .HasColumnName("patreon_user_id")
                        .HasColumnType("integer");

                    b.Property<string>("PatreonUserName")
                        .HasColumnName("patreon_user_name")
                        .HasColumnType("text");

                    b.Property<PatronStatus?>("PatronStatus")
                        .HasColumnName("patron_status")
                        .HasColumnType("patron_status");

                    b.Property<int>("Tier")
                        .HasColumnName("tier")
                        .HasColumnType("integer");

                    b.Property<int>("TierAmountCents")
                        .HasColumnName("tier_amount_cents")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id")
                        .HasName("pk_patreon");

                    b.ToTable("patreon");
                });

            modelBuilder.Entity("Rias.Database.Entities.ProfileEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("BackgroundDim")
                        .HasColumnName("background_dim")
                        .HasColumnType("integer");

                    b.Property<string>("BackgroundUrl")
                        .HasColumnName("background_url")
                        .HasColumnType("text");

                    b.Property<string[]>("Badges")
                        .HasColumnName("badges")
                        .HasColumnType("text[]");

                    b.Property<string>("Biography")
                        .HasColumnName("biography")
                        .HasColumnType("text");

                    b.Property<string>("Color")
                        .HasColumnName("color")
                        .HasColumnType("text");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id")
                        .HasName("pk_profile");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasName("ix_profile_user_id");

                    b.ToTable("profile");
                });

            modelBuilder.Entity("Rias.Database.Entities.SelfAssignableRoleEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("RoleId")
                        .HasColumnName("role_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("RoleName")
                        .HasColumnName("role_name")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_self_assignable_roles");

                    b.HasIndex("GuildId", "RoleId")
                        .IsUnique()
                        .HasName("ix_self_assignable_roles_guild_id_role_id");

                    b.ToTable("self_assignable_roles");
                });

            modelBuilder.Entity("Rias.Database.Entities.UserEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("Currency")
                        .HasColumnName("currency")
                        .HasColumnType("integer");

                    b.Property<DateTime>("DailyTaken")
                        .HasColumnName("daily_taken")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsBanned")
                        .HasColumnName("is_banned")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsBlacklisted")
                        .HasColumnName("is_blacklisted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastMessageDate")
                        .HasColumnName("last_message_date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Xp")
                        .HasColumnName("xp")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasName("ix_users_user_id");

                    b.ToTable("users");
                });

            modelBuilder.Entity("Rias.Database.Entities.VoteEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("Checked")
                        .HasColumnName("checked")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsWeekend")
                        .HasColumnName("is_weekend")
                        .HasColumnType("boolean");

                    b.Property<string>("Query")
                        .HasColumnName("query")
                        .HasColumnType("text");

                    b.Property<string>("Type")
                        .HasColumnName("type")
                        .HasColumnType("text");

                    b.Property<decimal>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id")
                        .HasName("pk_votes");

                    b.ToTable("votes");
                });

            modelBuilder.Entity("Rias.Database.Entities.WaifuEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int?>("CharacterId")
                        .HasColumnName("character_id")
                        .HasColumnType("integer");

                    b.Property<int?>("CustomCharacterId")
                        .HasColumnName("custom_character_id")
                        .HasColumnType("integer");

                    b.Property<string>("CustomImageUrl")
                        .HasColumnName("custom_image_url")
                        .HasColumnType("text");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsSpecial")
                        .HasColumnName("is_special")
                        .HasColumnType("boolean");

                    b.Property<int>("Position")
                        .HasColumnName("position")
                        .HasColumnType("integer");

                    b.Property<int>("Price")
                        .HasColumnName("price")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id")
                        .HasName("pk_waifus");

                    b.HasIndex("CharacterId")
                        .HasName("ix_waifus_character_id");

                    b.HasIndex("CustomCharacterId")
                        .HasName("ix_waifus_custom_character_id");

                    b.ToTable("waifus");
                });

            modelBuilder.Entity("Rias.Database.Entities.WarningEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("DateAdded")
                        .HasColumnName("date_added")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnName("guild_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ModeratorId")
                        .HasColumnName("moderator_id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Reason")
                        .HasColumnName("reason")
                        .HasColumnType("text");

                    b.Property<decimal>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id")
                        .HasName("pk_warnings");

                    b.ToTable("warnings");
                });

            modelBuilder.Entity("Rias.Database.Entities.WaifuEntity", b =>
                {
                    b.HasOne("Rias.Database.Entities.CharacterEntity", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterId")
                        .HasConstraintName("fk_waifus_characters_character_id")
                        .HasPrincipalKey("CharacterId");

                    b.HasOne("Rias.Database.Entities.CustomCharacterEntity", "CustomCharacter")
                        .WithMany()
                        .HasForeignKey("CustomCharacterId")
                        .HasConstraintName("fk_waifus_custom_characters_custom_character_id")
                        .HasPrincipalKey("CharacterId");
                });
#pragma warning restore 612, 618
        }
    }
}
