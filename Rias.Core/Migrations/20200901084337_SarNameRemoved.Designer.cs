﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Rias.Core.Database;
using Rias.Core.Models;

namespace Rias.Core.Migrations
{
    [DbContext(typeof(RiasDbContext))]
    [Migration("20200901084337_SarNameRemoved")]
    partial class SarNameRemoved
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasPostgresEnum(null, "last_charge_status", new[] { "paid", "declined", "pending", "refunded", "fraud", "other" })
                .HasPostgresEnum(null, "patron_status", new[] { "active_patron", "declined_patron", "former_patron" })
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.0-preview.8.20407.4");

            modelBuilder.Entity("Rias.Core.Database.Entities.CharactersEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("CharacterId")
                        .HasColumnType("integer")
                        .HasColumnName("character_id");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("text")
                        .HasColumnName("image_url");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("Url")
                        .HasColumnType("text")
                        .HasColumnName("url");

                    b.HasKey("Id")
                        .HasName("pk_characters");

                    b.HasAlternateKey("CharacterId")
                        .HasName("ak_characters_character_id");

                    b.HasIndex("CharacterId")
                        .IsUnique()
                        .HasDatabaseName("ix_characters_character_id");

                    b.ToTable("characters");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.CustomCharactersEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("CharacterId")
                        .HasColumnType("integer")
                        .HasColumnName("character_id");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("text")
                        .HasColumnName("image_url");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_custom_characters");

                    b.HasAlternateKey("CharacterId")
                        .HasName("ak_custom_characters_character_id");

                    b.HasIndex("CharacterId")
                        .IsUnique()
                        .HasDatabaseName("ix_custom_characters_character_id");

                    b.ToTable("custom_characters");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.CustomWaifusEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("text")
                        .HasColumnName("image_url");

                    b.Property<bool>("IsSpecial")
                        .HasColumnType("boolean")
                        .HasColumnName("is_special");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<int>("Position")
                        .HasColumnType("integer")
                        .HasColumnName("position");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_custom_waifus");

                    b.ToTable("custom_waifus");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.GuildUsersEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<bool>("IsMuted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_muted");

                    b.Property<DateTime>("LastMessageDate")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("last_message_date");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.Property<int>("Xp")
                        .HasColumnType("integer")
                        .HasColumnName("xp");

                    b.HasKey("Id")
                        .HasName("pk_guild_users");

                    b.HasIndex("GuildId", "UserId")
                        .IsUnique()
                        .HasDatabaseName("ix_guild_users_guild_id_user_id");

                    b.ToTable("guild_users");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.GuildXpRolesEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int>("Level")
                        .HasColumnType("integer")
                        .HasColumnName("level");

                    b.Property<decimal>("RoleId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("role_id");

                    b.HasKey("Id")
                        .HasName("pk_guild_xp_roles");

                    b.HasIndex("GuildId", "RoleId")
                        .IsUnique()
                        .HasDatabaseName("ix_guild_xp_roles_guild_id_role_id");

                    b.ToTable("guild_xp_roles");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.GuildsEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal>("AutoAssignableRoleId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("auto_assignable_role_id");

                    b.Property<string>("ByeMessage")
                        .HasColumnType("text")
                        .HasColumnName("bye_message");

                    b.Property<bool>("ByeNotification")
                        .HasColumnType("boolean")
                        .HasColumnName("bye_notification");

                    b.Property<decimal>("ByeWebhookId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("bye_webhook_id");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<bool>("DeleteCommandMessage")
                        .HasColumnType("boolean")
                        .HasColumnName("delete_command_message");

                    b.Property<string>("GreetMessage")
                        .HasColumnType("text")
                        .HasColumnName("greet_message");

                    b.Property<bool>("GreetNotification")
                        .HasColumnType("boolean")
                        .HasColumnName("greet_notification");

                    b.Property<decimal>("GreetWebhookId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("greet_webhook_id");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<string>("Locale")
                        .HasColumnType("text")
                        .HasColumnName("locale");

                    b.Property<decimal>("ModLogChannelId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("mod_log_channel_id");

                    b.Property<decimal>("MuteRoleId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("mute_role_id");

                    b.Property<string>("Prefix")
                        .HasColumnType("text")
                        .HasColumnName("prefix");

                    b.Property<int>("PunishmentWarningsRequired")
                        .HasColumnType("integer")
                        .HasColumnName("punishment_warnings_required");

                    b.Property<string>("WarningPunishment")
                        .HasColumnType("text")
                        .HasColumnName("warning_punishment");

                    b.Property<string>("XpLevelUpMessage")
                        .HasColumnType("text")
                        .HasColumnName("xp_level_up_message");

                    b.Property<string>("XpLevelUpRoleRewardMessage")
                        .HasColumnType("text")
                        .HasColumnName("xp_level_up_role_reward_message");

                    b.Property<bool>("XpNotification")
                        .HasColumnType("boolean")
                        .HasColumnName("xp_notification");

                    b.Property<decimal>("XpWebhookId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("xp_webhook_id");

                    b.HasKey("Id")
                        .HasName("pk_guilds");

                    b.HasIndex("GuildId")
                        .IsUnique()
                        .HasDatabaseName("ix_guilds_guild_id");

                    b.ToTable("guilds");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.MuteTimersEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<DateTime>("Expiration")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("expiration");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<decimal>("ModeratorId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("moderator_id");

                    b.Property<decimal>("MuteChannelSourceId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("mute_channel_source_id");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_mute_timers");

                    b.HasIndex("GuildId", "UserId")
                        .IsUnique()
                        .HasDatabaseName("ix_mute_timers_guild_id_user_id");

                    b.ToTable("mute_timers");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.PatreonEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("AmountCents")
                        .HasColumnType("integer")
                        .HasColumnName("amount_cents");

                    b.Property<bool>("Checked")
                        .HasColumnType("boolean")
                        .HasColumnName("checked");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<DateTimeOffset?>("LastChargeDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_charge_date");

                    b.Property<LastChargeStatus?>("LastChargeStatus")
                        .HasColumnType("last_charge_status")
                        .HasColumnName("last_charge_status");

                    b.Property<int>("PatreonUserId")
                        .HasColumnType("integer")
                        .HasColumnName("patreon_user_id");

                    b.Property<string>("PatreonUserName")
                        .HasColumnType("text")
                        .HasColumnName("patreon_user_name");

                    b.Property<PatronStatus?>("PatronStatus")
                        .HasColumnType("patron_status")
                        .HasColumnName("patron_status");

                    b.Property<int>("Tier")
                        .HasColumnType("integer")
                        .HasColumnName("tier");

                    b.Property<int>("TierAmountCents")
                        .HasColumnType("integer")
                        .HasColumnName("tier_amount_cents");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_patreon");

                    b.ToTable("patreon");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.ProfileEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("BackgroundDim")
                        .HasColumnType("integer")
                        .HasColumnName("background_dim");

                    b.Property<string>("BackgroundUrl")
                        .HasColumnType("text")
                        .HasColumnName("background_url");

                    b.Property<string[]>("Badges")
                        .HasColumnType("text[]")
                        .HasColumnName("badges");

                    b.Property<string>("Biography")
                        .HasColumnType("text")
                        .HasColumnName("biography");

                    b.Property<string>("Color")
                        .HasColumnType("text")
                        .HasColumnName("color");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_profile");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasDatabaseName("ix_profile_user_id");

                    b.ToTable("profile");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.SelfAssignableRolesEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<decimal>("RoleId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("role_id");

                    b.HasKey("Id")
                        .HasName("pk_self_assignable_roles");

                    b.HasIndex("GuildId", "RoleId")
                        .IsUnique()
                        .HasDatabaseName("ix_self_assignable_roles_guild_id_role_id");

                    b.ToTable("self_assignable_roles");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.UsersEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("Currency")
                        .HasColumnType("integer")
                        .HasColumnName("currency");

                    b.Property<DateTime>("DailyTaken")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("daily_taken");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<bool>("IsBanned")
                        .HasColumnType("boolean")
                        .HasColumnName("is_banned");

                    b.Property<bool>("IsBlacklisted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_blacklisted");

                    b.Property<DateTime>("LastMessageDate")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("last_message_date");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.Property<int>("Xp")
                        .HasColumnType("integer")
                        .HasColumnName("xp");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasDatabaseName("ix_users_user_id");

                    b.ToTable("users");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.VotesEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<bool>("Checked")
                        .HasColumnType("boolean")
                        .HasColumnName("checked");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<bool>("IsWeekend")
                        .HasColumnType("boolean")
                        .HasColumnName("is_weekend");

                    b.Property<string>("Query")
                        .HasColumnType("text")
                        .HasColumnName("query");

                    b.Property<string>("Type")
                        .HasColumnType("text")
                        .HasColumnName("type");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_votes");

                    b.ToTable("votes");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.WaifusEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<int?>("CharacterId")
                        .HasColumnType("integer")
                        .HasColumnName("character_id");

                    b.Property<int?>("CustomCharacterId")
                        .HasColumnType("integer")
                        .HasColumnName("custom_character_id");

                    b.Property<string>("CustomImageUrl")
                        .HasColumnType("text")
                        .HasColumnName("custom_image_url");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<bool>("IsSpecial")
                        .HasColumnType("boolean")
                        .HasColumnName("is_special");

                    b.Property<int>("Position")
                        .HasColumnType("integer")
                        .HasColumnName("position");

                    b.Property<int>("Price")
                        .HasColumnType("integer")
                        .HasColumnName("price");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_waifus");

                    b.HasIndex("CharacterId")
                        .HasDatabaseName("ix_waifus_character_id");

                    b.HasIndex("CustomCharacterId")
                        .HasDatabaseName("ix_waifus_custom_character_id");

                    b.ToTable("waifus");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.WarningsEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<decimal>("ModeratorId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("moderator_id");

                    b.Property<string>("Reason")
                        .HasColumnType("text")
                        .HasColumnName("reason");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_warnings");

                    b.ToTable("warnings");
                });

            modelBuilder.Entity("Rias.Core.Database.Entities.WaifusEntity", b =>
                {
                    b.HasOne("Rias.Core.Database.Entities.CharactersEntity", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterId")
                        .HasConstraintName("fk_waifus_characters_character_id")
                        .HasPrincipalKey("CharacterId");

                    b.HasOne("Rias.Core.Database.Entities.CustomCharactersEntity", "CustomCharacter")
                        .WithMany()
                        .HasForeignKey("CustomCharacterId")
                        .HasConstraintName("fk_waifus_custom_characters_custom_character_id")
                        .HasPrincipalKey("CharacterId");
                });
#pragma warning restore 612, 618
        }
    }
}
