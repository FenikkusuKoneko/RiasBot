using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rias.Core.Database;
using Serilog;

namespace Rias.Core.Implementation
{
    public class Localization
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _locales =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
        
        private readonly ConcurrentDictionary<Snowflake, string> _guildLocales = new ConcurrentDictionary<Snowflake, string>();
        
        private readonly string _localesPath = Path.Combine(Environment.CurrentDirectory, "assets/locales");
        private readonly string _defaultLocale = "en";

        public Localization(IServiceProvider serviceProvider)
        {
            var sw = Stopwatch.StartNew();
            
            foreach (var locale in Directory.GetFiles(_localesPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(locale);
                _locales.TryAdd(fileName, JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText(locale)));
            }
            
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();

            var guildLocalesDb = db.Guilds.Where(guildDb => !string.IsNullOrEmpty(guildDb.Locale)).ToList();
            foreach (var localeDb in guildLocalesDb)
            {
                _guildLocales.TryAdd(localeDb.GuildId, localeDb.Locale!);
            }
            
            sw.Stop();
            Log.Information($"Locales loaded: {sw.ElapsedMilliseconds} ms");
        }
        
        public void SetGuildLocale(Snowflake guildId, string locale)
        {
            _guildLocales.AddOrUpdate(guildId, locale, (id, old) => locale);
        }
        
        public string GetGuildLocale(Snowflake? guildId)
        {
            if (!guildId.HasValue)
                return _defaultLocale;

            return _guildLocales.TryGetValue(guildId.Value, out var locale) ? locale : _defaultLocale;
        }

        public void RemoveGuildLocale(Snowflake guildId)
        {
            _guildLocales.TryRemove(guildId, out _);
        }

        private bool TryGetLocaleString(string locale, string key, out string? value)
        {
            if (_locales.TryGetValue(locale, out var localeDictionary))
            {
                if (localeDictionary.TryGetValue(key, out var @string))
                {
                    value = @string;
                    return true;
                }
            }

            value = null;
            return false;
        }
        
        /// <summary>
        /// Get a translation string with or without arguments.<br/>
        /// </summary>
        public string GetText(Snowflake? guildId, string key, params object[] args)
        {
            var locale = guildId.HasValue ? GetGuildLocale(guildId.Value) : _defaultLocale;
            if (TryGetLocaleString(locale, key, out var @string) && !string.IsNullOrEmpty(@string))
                return string.Format(@string!, args);

            if (!string.Equals(locale, _defaultLocale)
                && TryGetLocaleString(_defaultLocale, key, out @string))
                return string.Format(@string!, args);

            throw new InvalidOperationException($"The translation for the key \"{key}\" couldn't be found.");
        }
        
        public readonly (string Locale, string Language)[] Locales =
        {
            ("EN", "English")
        };
        
        public static string AdministrationGreetEnabled => "administration_greet_enabled";
        public static string AdministrationGreetDisabled => "administration_greet_disabled";
        public static string AdministrationGreetMessageNotSet => "administration_greet_message_not_set";
        public static string AdministrationGreetMessageLengthLimit => "administration_greet_message_length_limit";
        public static string AdministrationGreetMessageSet => "administration_greet_message_set";
        public static string AdministrationByeEnabled => "administration_bye_enabled";
        public static string AdministrationByeDisabled => "administration_bye_disabled";
        public static string AdministrationByeMessageNotSet => "administration_bye_message_not_set";
        public static string AdministrationByeMessageLengthLimit => "administration_bye_message_length_limit";
        public static string AdministrationByeMessageSet => "administration_bye_message_set";
        public static string AdministrationModLogEnabled => "administration_mod_log_enabled";
        public static string AdministrationModLogDisabled => "administration_mod_log_disabled";
        public static string AdministrationCategoryChannelCreated => "administration_category_channel_created";
        public static string AdministrationCategoryChannelDeleted => "administration_category_channel_deleted";
        public static string AdministrationCategoryChannelRenamed => "administration_category_channel_renamed";
        public static string AdministrationChannelNameLengthLimit => "administration_channel_name_length_limit";
        public static string AdministrationCategoryChannelNotFound => "administration_category_channel_not_found";
        public static string AdministrationTextChannelNotFound => "administration_text_channel_not_found";
        public static string AdministrationVoiceChannelNotFound => "administration_voice_channel_not_found";
        public static string AdministrationCategoryChannelNoViewPermission => "administration_category_channel_no_view_permission";
        public static string AdministrationTextChannelNoViewPermission => "administration_text_channel_no_view_permission";
        public static string AdministrationVoiceChannelNoViewPermission => "administration_voice_channel_no_view_permission";
        public static string AdministrationTextChannelAddedToCategory => "administration_text_channel_added_to_category";
        public static string AdministrationVoiceChannelAddedToCategory => "administration_voice_channel_added_to_category";
        public static string AdministrationEmojiCreated => "administration_emoji_created";
        public static string AdministrationEmojiDeleted => "administration_emoji_deleted";
        public static string AdministrationEmojiRenamed => "administration_emoji_renamed";
        public static string AdministrationAnimatedEmojisLimit => "administration_animated_emojis_limit";
        public static string AdministrationStaticEmojisLimit => "administration_static_emojis_limit";
        public static string AdministrationEmojiSizeLimit => "administration_emoji_size_limit";
        public static string AdministrationEmojiNotFound => "administration_emoji_not_found";
        public static string AdministrationEmojiNotDeleted => "administration_emoji_not_deleted";
        public static string AdministrationEmojiNotRenamed => "administration_emoji_not_renamed";
        public static string AdministrationCannotKickOwner => "administration_cannot_kick_owner";
        public static string AdministrationCannotBanOwner => "administration_cannot_ban_owner";
        public static string AdministrationCannotSoftbanOwner => "administration_cannot_softban_owner";
        public static string AdministrationCannotPrunebanOwner => "administration_cannot_pruneban_owner";
        public static string AdministrationUserAboveMe => "administration_user_above_me";
        public static string AdministrationYouAboveMe => "administration_you_above_me";
        public static string AdministrationUserAbove => "administration_user_above";
        public static string AdministrationUserKicked => "administration_user_kicked";
        public static string AdministrationKickedFrom => "administration_kicked_from";
        public static string AdministrationUserBanned => "administration_user_banned";
        public static string AdministrationBannedFrom => "administration_banned_from";
        public static string AdministrationUserSoftBanned => "administration_user_soft_banned";
        public static string AdministrationPruneLimit => "administration_prune_limit";
        public static string AdministrationCannotMuteOwner => "administration_cannot_mute_owner";
        public static string AdministrationMuteTimeoutLowest => "administration_mute_timeout_lowest";
        public static string AdministrationMuteTimeoutHighest => "administration_mute_timeout_highest";
        public static string AdministrationUserAlreadyMuted => "administration_user_already_muted";
        public static string AdministrationUserNotMuted => "administration_user_not_muted";
        public static string AdministrationUserMuted => "administration_user_muted";
        public static string AdministrationUserUnmuted => "administration_user_unmuted";
        public static string AdministrationMuteRoleNotFound => "administration_mute_role_not_found";
        public static string AdministrationNewMuteRoleSet => "administration_new_mute_role_set";
        public static string AdministrationMuteRoleNotSet => "administration_mute_role_not_set";
        public static string AdministrationMuteRoleAbove => "administration_mute_role_above";
        public static string AdministrationMutedFor => "administration_muted_for";
        public static string AdministrationUserNotFound => "administration_user_not_found";
        public static string AdministrationRoleNotFound => "administration_role_not_found";
        public static string AdministrationRoleAboveMe => "administration_role_above_me";
        public static string AdministrationRoleAbove => "administration_role_above";
        public static string AdministrationNoRoles => "administration_no_roles";
        public static string AdministrationUserNoRoles => "administration_user_no_roles";
        public static string AdministrationRolesList => "administration_roles_list";
        public static string AdministrationUserRolesList => "administration_user_roles_list";
        public static string AdministrationRoleCreated => "administration_role_created";
        public static string AdministrationRoleDeleted => "administration_role_deleted";
        public static string AdministrationRoleNotDeleted => "administration_role_not_deleted";
        public static string AdministrationRoleColorChanged => "administration_role_color_changed";
        public static string AdministrationRoleRenamed => "administration_role_renamed";
        public static string AdministrationRoleDisplayed => "administration_role_displayed";
        public static string AdministrationRoleNotDisplayed => "administration_role_not_displayed";
        public static string AdministrationRoleMentionable => "administration_role_mentionable";
        public static string AdministrationRoleNotMentionable => "administration_role_not_mentionable";
        public static string AdministrationUserHasRole => "administration_user_has_role";
        public static string AdministrationUserNoRole => "administration_user_no_role";
        public static string AdministrationRoleAdded => "administration_role_added";
        public static string AdministrationRoleNotAdded => "administration_role_not_added";
        public static string AdministrationRoleRemoved => "administration_role_removed";
        public static string AdministrationRoleNotRemoved => "administration_role_not_removed";
        public static string AdministrationAarDisabled => "administration_aar_disabled";
        public static string AdministrationAarSet => "administration_aar_set";
        public static string AdministrationAarNotSet => "administration_aar_not_set";
        public static string AdministrationRoleNotSelfAssignable => "administration_role_not_self_assignable";
        public static string AdministrationYouAre => "administration_you_are";
        public static string AdministrationYouAlreadyAre => "administration_you_already_are";
        public static string AdministrationYouAreNot => "administration_you_are_not";
        public static string AdministrationSarAdded => "administration_sar_added";
        public static string AdministrationSarNotAdded => "administration_sar_not_added";
        public static string AdministrationSarRemoved => "administration_sar_removed";
        public static string AdministrationSarInList => "administration_sar_in_list";
        public static string AdministrationSarNotInList => "administration_sar_not_in_list";
        public static string AdministrationNoSar => "administration_no_sar";
        public static string AdministrationSarList => "administration_sar_list";
        public static string AdministrationNicknameOwner => "administration_nickname_owner";
        public static string AdministrationNicknameYouOwner => "administration_nickname_you_owner";
        public static string AdministrationChangeNicknamePermission => "administration_change_nickname_permission";
        public static string AdministrationNicknameRemoved => "administration_nickname_removed";
        public static string AdministrationYourNicknameRemoved => "administration_your_nickname_removed";
        public static string AdministrationNicknameChanged => "administration_nickname_changed";
        public static string AdministrationYourNicknameChanged => "administration_your_nickname_changed";
        public static string AdministrationServerNameLengthLimit => "administration_server_name_length_limit";
        public static string AdministrationServerNameChanged => "administration_server_name_changed";
        public static string AdministrationServerIconChanged => "administration_server_icon_changed";
        public static string AdministrationTextChannelCreated => "administration_text_channel_created";
        public static string AdministrationTextChannelDeleted => "administration_text_channel_deleted";
        public static string AdministrationTextChannelRenamed => "administration_text_channel_renamed";
        public static string AdministrationChannelNoTopic => "administration_channel_no_topic";
        public static string AdministrationChannelTopicTitle => "administration_channel_topic_title";
        public static string AdministrationChannelTopicLengthLimit => "administration_channel_topic_length_limit";
        public static string AdministrationChannelTopicSet => "administration_channel_topic_set";
        public static string AdministrationChannelTopicRemoved => "administration_channel_topic_removed";
        public static string AdministrationCurrentChannelNsfwEnabled => "administration_current_channel_nsfw_enabled";
        public static string AdministrationCurrentChannelNsfwDisabled => "administration_current_channel_nsfw_disabled";
        public static string AdministrationChannelNsfwEnabled => "administration_channel_nsfw_enabled";
        public static string AdministrationChannelNsfwDisabled => "administration_channel_nsfw_disabled";
        public static string AdministrationVoiceChannelCreated => "administration_voice_channel_created";
        public static string AdministrationVoiceChannelDeleted => "administration_voice_channel_deleted";
        public static string AdministrationVoiceChannelRenamed => "administration_voice_channel_renamed";
        public static string AdministrationWarningUserTypeNoPermissionsDefault(string userType) => $"administration_warning_{userType}_no_permissions_default";
        public static string AdministrationWarningUserTypeNoPermissionsPunishment(string userType) => $"administration_warning_{userType}_no_permissions_punishment";
        public static string AdministrationCannotWarnOwner => "administration_cannot_warn_owner";
        public static string AdministrationUserWarningsLimit => "administration_user_warnings_limit";
        public static string AdministrationWarn => "administration_warn";
        public static string AdministrationWarningNumber => "administration_warning_number";
        public static string AdministrationWarningMute => "administration_warning_mute";
        public static string AdministrationWarningKick => "administration_warning_kick";
        public static string AdministrationWarningBan => "administration_warning_ban";
        public static string AdministrationWarnedUsers => "administration_warned_users";
        public static string AdministrationNoWarnedUsers => "administration_no_warned_users";
        public static string AdministrationWarningListFooter => "administration_warning_list_footer";
        public static string AdministrationUserWarnings => "administration_user_warnings";
        public static string AdministrationUserNoWarnings => "administration_user_no_warnings";
        public static string AdministrationClearWarningUserNoPermissions => "administration_clear_warning_user_no_permissions";
        public static string AdministrationClearWarningIndexAbove => "administration_clear_warning_index_above";
        public static string AdministrationClearWarningNotUserWarning => "administration_clear_warning_not_user_warning";
        public static string AdministrationClearWarningNotModWarnings => "administration_clear_warning_not_mod_warnings";
        public static string AdministrationWarningCleared => "administration_warning_cleared";
        public static string AdministrationWarningsCleared => "administration_warnings_cleared";
        public static string AdministrationAllWarningsCleared => "administration_all_warnings_cleared";
        public static string AdministrationWarningPunishment => "administration_warning_punishment";
        public static string AdministrationNoWarningPunishment => "administration_no_warning_punishment";
        public static string AdministrationWarningsLimit => "administration_warnings_limit";
        public static string AdministrationWarningPunishmentRemoved => "administration_warning_punishment_removed";
        public static string AdministrationWarningInvalidPunishment => "administration_warning_invalid_punishment";
        public static string AdministrationWarningPunishmentSet => "administration_warning_punishment_set";
        public static string AdministrationPermission(string type) => $"administration_{type}_permission";
        public static string AdministrationModerator => "administration_moderator";
        public static string AdministrationWarning => "administration_warning";
        public static string AdministrationWarnings => "administration_warnings";
        public static string AdministrationPunishment => "administration_punishment";
         
        public static string BotGuildNotFound => "bot_guild_not_found";
        public static string BotLeftGuild => "bot_left_guild";
        public static string BotShutdown => "bot_shutdown";
        public static string BotUpdate => "bot_update";
        public static string BotChannelNotTextChannel => "bot_channel_not_text_channel";
        public static string BotTextChannelNoSendMessagesPermission => "bot_text_channel_no_send_messages_permission";
        public static string BotUserIsBot => "bot_user_is_bot";
        public static string BotUserMessageNotSent => "bot_user_message_not_sent";
        public static string BotMessageSent => "bot_message_sent";
        public static string BotChannelMessageIdsBadFormat => "bot_channel_message_ids_bad_format";
        public static string BotMessageNotFound => "bot_message_not_found";
        public static string BotMessageNotUserMessage => "bot_message_not_user_message";
        public static string BotMessageNotSelf => "bot_message_not_self";
        public static string BotMessageEdited => "bot_message_edited";
        public static string BotMutualGuilds => "bot_mutual_guilds";
        public static string BotRoslynCompiler => "bot_roslyn_compiler";
        public static string BotEvaluatingCode => "bot_evaluating_code";
        public static string BotCodeEvaluated => "bot_code_evaluated";
        public static string BotCodeEvaluatedWithError => "bot_code_evaluated_with_error";
        public static string BotCodeCompiledWithError => "bot_code_compiled_with_error";
        public static string BotCompilationTime => "bot_compilation_time";
        public static string BotExecutionTime => "bot_execution_time";
        public static string BotCode => "bot_code";
        public static string BotError => "bot_error";
        public static string BotActivitySet => "bot_activity_set";
        public static string BotActivityRemoved => "bot_activity_removed";
        public static string BotActivity(string type) => $"bot_activity_{type}";
        public static string BotActivityRotationSet => "bot_activity_rotation_set";
        public static string BotActivityRotationLimit => "bot_activity_rotation_limit";
        public static string BotStatusSet => "bot_status_set";
        public static string BotStatus(string status) => $"bot_status_{status}";
        public static string BotUsernameLengthLimit => "bot_username_length_limit";
        public static string BotSetUsernameDialog => "bot_set_username_dialog";
        public static string BotSetUsernameCanceled => "bot_set_username_canceled";
        public static string BotUsernameSet => "bot_username_set";
        public static string BotSetUsernameError => "bot_set_username_error";
        public static string BotAvatarSet => "bot_avatar_set";
        public static string BotSetAvatarError => "bot_set_avatar_error";
        public static string BotDeleteDialog => "bot_delete_dialog";
        public static string BotDeleteCanceled => "bot_delete_canceled";
        public static string BotUserDeleted => "bot_user_deleted";
        public static string BotUserNotInDatabase => "bot_user_not_in_database";
        public static string BotIsBlacklisted => "bot_is_blacklisted";
        public static string BotIsBanned => "bot_is_banned";
        public static string BotBlacklistDialog => "bot_blacklist_dialog";
        public static string BotBlacklistCanceled => "bot_blacklist_canceled";
        public static string BotUserBlacklisted => "bot_user_blacklisted";
        public static string BotUserBlacklistRemoved => "bot_user_blacklist_removed";
        public static string BotBotBanDialog => "bot_bot_ban_dialog";
        public static string BotBotBanCanceled => "bot_bot_ban_canceled";
        public static string BotUserBotBanned => "bot_user_bot_banned";
        public static string BotUserBotBanRemoved => "bot_user_bot_ban_removed";
        public static string BotDatabase => "bot_database";
        
        public static string CommandsDeleteCommandMessageEnabled => "commands_delete_command_message_enabled";
        public static string CommandsDeleteCommandMessageDisabled => "commands_delete_command_message_disabled";
        
        public static string GamblingBetLessThan => "gambling_bet_less_than";
        public static string GamblingBetMoreThan => "gambling_bet_more_than";
        public static string GamblingCurrencyNotEnough => "gambling_currency_not_enough";
        public static string GamblingYouWon => "gambling_you_won";
        public static string GamblingYouLost => "gambling_you_lost";
        public static string GamblingBlackjackDraw => "gambling_blackjack_draw";
        public static string GamblingBlackjack => "gambling_blackjack";
        public static string GamblingBlackjackSession => "gambling_blackjack_session";
        public static string GamblingBlackjackNoSession => "gambling_blackjack_no_session";
        public static string GamblingBlackjackStopped => "gambling_blackjack_stopped";
        public static string GamblingBet => "gambling_bet";
        public static string GamblingCurrency => "gambling_currency";
        public static string GamblingHearts => "gambling_hearts";
        public static string GamblingCurrencyYou => "gambling_currency_you";
        public static string GamblingCurrencyYouVote => "gambling_currency_you_vote";
        public static string GamblingCurrencyUser => "gambling_currency_user";
        public static string GamblingUserRewarded => "gambling_user_rewarded";
        public static string GamblingUserTook => "gambling_user_took";
        public static string GamblingCurrencyLeaderboard => "gambling_currency_leaderboard";
        public static string GamblingLeaderboardNoUsers => "gambling_leaderboard_no_users";
        public static string GamblingDailyWait => "gambling_daily_wait";
        public static string GamblingDailyWaitVote => "gambling_daily_wait_vote";
        public static string GamblingDailyReceived => "gambling_daily_received";
        public static string GamblingDailyReceivedVote => "gambling_daily_received_vote";
        public static string GamesRpsWon => "games_rps_won";
        public static string GamesRpsLost => "games_rps_lost";
        public static string GamesRpsDraw => "games_rps_draw";
        public static string GamesEightBallAnswer(int number) => $"games_eight_ball_answer_{number}";
        
        public static string HelpTitle => "help_title";
        public static string HelpInfo => "help_info";
        public static string HelpLinks => "help_links";
        public static string HelpSupportServer => "help_support_server";
        public static string HelpInviteMe => "help_invite_me";
        public static string HelpWebsite => "help_website";
        public static string HelpDonate => "help_donate";
        public static string HelpCommandNotFound => "help_command_not_found";
        public static string HelpRequiresUserPermission => "help_requires_user_permission";
        public static string HelpRequiresBotPermission => "help_requires_bot_permission";
        public static string HelpRequiresOwner => "help_requires_owner";
        public static string HelpModule => "help_module";
        public static string HelpModulesListTitle => "help_modules_list_title";
        public static string HelpModulesListFooter => "help_modules_list_footer";
        public static string HelpModuleNotFound => "help_module_not_found";
        public static string HelpAllCommandsForModule => "help_all_commands_for_module";
        public static string HelpAllCommandsForSubmodule => "help_all_commands_for_submodule";
        public static string HelpCommandInfo => "help_command_info";
        public static string HelpAllCommands => "help_all_commands";
        public static string HelpCommandsNotSent => "help_commands_not_sent";
        
        public static string NsfwChannelNotNsfw => "nsfw_channel_not_nsfw";
        public static string NsfwCacheNotInitialized => "nsfw_cache_not_initialized";
        public static string NsfwNoHentai => "nsfw_no_hentai";

        public static string ProfileDefaultBiography => "profile_default_biography";
        public static string ProfileBackgroundPreview => "profile_background_preview";
        public static string ProfileBackgroundSet => "profile_background_set";
        public static string ProfileBackgroundCanceled => "profile_background_canceled";
        public static string ProfileBackgroundDimBetween => "profile_background_dim_between";
        public static string ProfileBackgroundDimSet => "profile_background_dim_set";
        public static string ProfileBiographyLimit => "profile_biography_limit";
        public static string ProfileBiographySet => "profile_biography_set";
        public static string ProfileColorSet => "profile_color_set";
        public static string ProfileInvalidColor => "profile_invalid_color";
        public static string ProfileFirstBadgeNoPatreon => "profile_first_badge_no_patreon";
        public static string ProfileSecondBadgeNoPatreon => "profile_second_badge_no_patreon";
        public static string ProfileThirdBadgeNoPatreon => "profile_third_badge_no_patreon";
        public static string ProfileBadgeError => "profile_badge_error";
        public static string ProfileBadgeNotAvailable => "profile_badge_not_available";
        public static string ProfileBadgeTextLimit => "profile_badge_text_limit";
        public static string ProfileBadgeSet => "profile_badge_set";
        public static string ProfileBadgeRemoved => "profile_badge_removed";
        
        public static string ReactionsNoWeebApi => "reactions_no_weeb_api";
        public static string ReactionsPoweredBy => "reactions_powered_by";
        public static string ReactionsLimit => "reactions_limit";
        public static string ReactionsPatYou => "reactions_pat_you";
        public static string ReactionsPattedBy => "reactions_patted_by";
        public static string ReactionsHugYou => "reactions_hug_you";
        public static string ReactionsHuggedBy => "reactions_hugged_by";
        public static string ReactionsKissYou => "reactions_kiss_you";
        public static string ReactionsKissedBy => "reactions_kissed_by";
        public static string ReactionsLickYou => "reactions_lick_you";
        public static string ReactionsLickedBy => "reactions_licked_by";
        public static string ReactionsCuddleYou => "reactions_cuddle_you";
        public static string ReactionsCuddledBy => "reactions_cuddled_by";
        public static string ReactionsBiteYou => "reactions_bite_you";
        public static string ReactionsBittenBy => "reactions_bitten_by";
        public static string ReactionsSlapYou => "reactions_slap_you";
        public static string ReactionsSlappedBy => "reactions_slapped_by";
        public static string ReactionsDontCry => "reactions_dont_cry";
        public static string ReactionsGropeYou => "reactions_grope_you";
        public static string ReactionsGropedBy => "reactions_groped_by";
        public static string ReactionsBlush => "reactions_blush";
        public static string ReactionsBlushAt => "reactions_blush_at";
        public static string ReactionsDance => "reactions_dance";
        public static string ReactionsDanceTogether => "reactions_dance_together";
        public static string ReactionsPokeYou => "reactions_poke_you";
        public static string ReactionsPokedBy => "reactions_poked_by";
        public static string ReactionsPout => "reactions_pout";
        public static string ReactionsPoutAt => "reactions_pout_at";
        public static string ReactionsSleepy => "reactions_sleepy";
        
        public static string SearchesSource => "searches_source";
        public static string SearchesNotFound => "searches_not_found";
        public static string SearchesUrbanDictionaryNoApiKey => "searches_urban_dictionary_no_api_key";
        public static string SearchesDefinitionNotFound => "searches_definition_not_found";
        public static string SearchesAnimeNotFound => "searches_anime_not_found";
        public static string SearchesMangaNotFound => "searches_manga_not_found";
        public static string SearchesCharacterNotFound => "searches_character_not_found";
        public static string SearchesAnimeListNotFound => "searches_anime_list_not_found";
        public static string SearchesMangaListNotFound => "searches_manga_list_not_found";
        public static string SearchesCharactersNotFound => "searches_characters_not_found";
        public static string SearchesTitleRomaji => "searches_title_romaji";
        public static string SearchesTitleEnglish => "searches_title_english";
        public static string SearchesTitleNative => "searches_title_native";
        public static string SearchesFormat => "searches_format";
        public static string SearchesEpisodes => "searches_episodes";
        public static string SearchesEpisodeDuration => "searches_episode_duration";
        public static string SearchesChapters => "searches_chapters";
        public static string SearchesVolumes => "searches_volumes";
        public static string SearchesStartDate => "searches_start_date";
        public static string SearchesEndDate => "searches_end_date";
        public static string SearchesSeason => "searches_season";
        public static string SearchesAverageScore => "searches_average_score";
        public static string SearchesMeanScore => "searches_mean_score";
        public static string SearchesPopularity => "searches_popularity";
        public static string SearchesFavourites => "searches_favourites";
        public static string SearchesGenres => "searches_genres";
        public static string SearchesSynonyms => "searches_synonyms";
        public static string SearchesIsAdult => "searches_is_adult";
        public static string SearchesDescription => "searches_description";
        public static string SearchesFirstName => "searches_first_name";
        public static string SearchesLastName => "searches_last_name";
        public static string SearchesNativeName => "searches_native_name";
        public static string SearchesAlternative => "searches_alternative";
        public static string SearchesFromManga => "searches_from_manga";
        public static string SearchesFromAnime => "searches_from_anime";
        public static string SearchesAnimeList => "searches_anime_list";
        public static string SearchesMangaList => "searches_manga_list";
        public static string SearchesCharacterList => "searches_character_list";
        public static string SearchesNeko => "searches_neko";
        public static string SearchesKitsune => "searches_kitsune";
        
        public static string UtilityUrlNotValid => "utility_url_not_valid";
        public static string UtilityUrlNotHttps => "utility_url_not_https";
        public static string UtilityUrlNotPngJpg => "utility_url_not_png_jpg";
        public static string UtilityUrlNotPngJpgGif => "utility_url_not_png_jpg_gif";
        public static string UtilityImageOrUrlNotGood => "utility_image_or_url_not_good";
        public static string UtilityStatus => "utility_status";
        public static string UtilityPrefixIs => "utility_prefix_is";
        public static string UtilityPrefixLimit => "utility_prefix_limit";
        public static string UtilityPrefixChanged => "utility_prefix_changed";
        public static string UtilityLanguages => "utility_languages";
        public static string UtilityLanguagesFooter => "utility_languages_footer";
        public static string UtilityLanguageNotFound => "utility_language_not_found";
        public static string UtilityLanguageSet => "utility_language_set";
        public static string UtilityInviteInfo => "utility_invite_info";
        public static string UtilityPatreonInfo => "utility_patreon_info";
        public static string UtilityNoPatrons => "utility_no_patrons";
        public static string UtilityAllPatrons => "utility_all_patrons";
        public static string UtilityVoteInfo => "utility_vote_info";
        public static string UtilityNoVotes => "utility_no_votes";
        public static string UtilityAllVotes => "utility_all_votes";
        public static string UtilityVotes => "utility_votes";
        public static string UtilityPingInfo => "utility_ping_info";
        public static string UtilityChose => "utility_chose";
        public static string UtilityCalculator => "utility_calculator";
        public static string UtilityExpression => "utility_expression";
        public static string UtilityExpressionFailed => "utility_expression_failed";
        public static string UtilityResult => "utility_result";
        public static string UtilityStats => "utility_stats";
        public static string UtilityAuthor => "utility_author";
        public static string UtilityBotId => "utility_bot_id";
        public static string UtilityMasterId => "utility_master_id";
        public static string UtilityShard => "utility_shard";
        public static string UtilityInServer => "utility_in_server";
        public static string UtilityCommandsAttempted => "utility_commands_attempted";
        public static string UtilityCommandsExecuted => "utility_commands_executed";
        public static string UtilityUptime => "utility_uptime";
        public static string UtilityPresence => "utility_presence";
        public static string UtilityServers => "utility_servers";
        public static string UtilityTextChannels => "utility_text_channels";
        public static string UtilityVoiceChannels => "utility_voice_channels";
        public static string UtilityUsername => "utility_username";
        public static string UtilityNickname => "utility_nickname";
        public static string UtilityActivity => "utility_activity";
        public static string UtilityJoinedServer => "utility_joined_server";
        public static string UtilityJoinedDiscord => "utility_joined_discord";
        public static string UtilityRoles => "utility_roles";
        public static string UtilityOwner => "utility_owner";
        public static string UtilityCurrentlyOnline => "utility_currently_online";
        public static string UtilityBots => "utility_bots";
        public static string UtilityCreatedAt => "utility_created_at";
        public static string UtilitySystemChannel => "utility_system_channel";
        public static string UtilityAfkChannel => "utility_afk_channel";
        public static string UtilityRegion => "utility_region";
        public static string UtilityVerificationLevel => "utility_verification_level";
        public static string UtilityBoostTier => "utility_boost_tier";
        public static string UtilityBoosts => "utility_boosts";
        public static string UtilityVanityUrl => "utility_vanity_url";
        public static string UtilityFeatures => "utility_features";
        public static string UtilityEmojis => "utility_emojis";
        public static string UtilityShardsInfo => "utility_shards_info";
        public static string UtilityShardState => "utility_shard_state";
        public static string UtilityUsersPlay => "utility_users_play";
        public static string UtilityNoUserIsPlaying => "utility_no_user_is_playing";
        public static string UtilityPrice => "utility_price";
        public static string UtilityTimeLowest => "utility_time_lowest";
        public static string UtilityTimeHighest => "utility_time_highest";
        public static string UtilityConverter => "utility_converter";
        public static string UtilityUnitNotFound => "utility_unit_not_found";
        public static string UtilityUnitNotFoundInCategory => "utility_unit_not_found_in_category";
        public static string UtilityUnitsCategoryNotFound => "utility_units_category_not_found";
        public static string UtilityUnitsNotCompatible => "utility_units_not_compatible";
        public static string UtilityAllUnitsCategories => "utility_all_units_categories";
        public static string UtilityCategoryAllUnits => "utility_category_all_units";
        public static string UtilityConvertListFooter => "utility_convert_list_footer";
        
        public static string WaifuHasWaifu => "waifu_has_waifu";
        public static string WaifuClaimCurrencyNotEnough => "waifu_claim_currency_not_enough";
        public static string WaifuClaimConfirmation => "waifu_claim_confirmation";
        public static string WaifuClaimedBy => "waifu_claimed_by";
        public static string WaifuClaimNote => "waifu_claim_note";
        public static string WaifuClaimCanceled => "waifu_claim_canceled";
        public static string WaifuWaifuClaimed => "waifu_waifu_claimed";
        public static string WaifuNotFound => "waifu_not_found";
        public static string WaifuDivorceConfirmation => "waifu_divorce_confirmation";
        public static string WaifuDivorceCanceled => "waifu_divorce_canceled";
        public static string WaifuDivorced => "waifu_divorced";
        public static string WaifuNoWaifus => "waifu_no_waifus";
        public static string WaifuUserNoWaifus => "waifu_user_no_waifus";
        public static string WaifuAllWaifus => "waifu_all_waifus";
        public static string WaifuAllUserWaifus => "waifu_all_user_waifus";
        public static string WaifuWaifusNumber => "waifu_waifus_number";
        public static string WaifuPosition => "waifu_position";
        public static string WaifuAlreadySpecial => "waifu_already_special";
        public static string WaifuSpecialConfirmation => "waifu_special_confirmation";
        public static string WaifuSpecialCanceled => "waifu_special_canceled";
        public static string WaifuSpecial => "waifu_special";
        public static string WaifuImageSetError => "waifu_image_set_error";
        public static string WaifuImageSet => "waifu_image_set";
        public static string WaifuPositionLowerLimit => "waifu_position_lower_limit";
        public static string WaifuPositionHigherLimit => "waifu_position_higher_limit";
        public static string WaifuHasPosition => "waifu_has_position";
        public static string WaifuPositionSet => "waifu_position_set";
        public static string WaifuCreationConfirmation => "waifu_creation_confirmation";
        public static string WaifuCreationCanceled => "waifu_creation_canceled";
        public static string WaifuCreated => "waifu_created";
        public static string WaifuWaifus => "waifu_waifus";
        
        public static string XpGlobalLevel => "xp_global_level";
        public static string XpGlobalXp => "xp_global_xp";
        public static string XpLevel => "xp_level";
        public static string XpLevelX => "xp_level_x";
        public static string XpXp => "xp_xp";
        public static string XpTotalXp => "xp_total_xp";
        public static string XpLvl => "xp_lvl";
        public static string XpLeaderboardEmpty => "xp_leaderboard_empty";
        public static string XpGuildLeaderboardEmpty => "xp_guild_leaderboard_empty";
        public static string XpLeaderboard => "xp_leaderboard";
        public static string XpGuildLeaderboard => "xp_guild_leaderboard";
        public static string XpNotificationEnabled => "xp_notification_enabled";
        public static string XpNotificationDisabled => "xp_notification_disabled";
        public static string XpLevelUpRoleRewardLimit => "xp_level_up_role_reward_limit";
        public static string XpLevelUpRoleRewardRemoved => "xp_level_up_role_reward_removed";
        public static string XpLevelUpRoleRewardNotSet => "xp_level_up_role_reward_not_set";
        public static string XpLevelUpRoleRewardSet => "xp_level_up_role_reward_set";
        public static string XpNoLevelUpRoleReward => "xp_no_level_up_role_reward";
        public static string XpLevelUpRoleRewardList => "xp_level_up_role_reward_list";
        public static string XpResetGuildXpConfirmation => "xp_reset_guild_xp_confirmation";
        public static string XpResetGuildXpCanceled => "xp_reset_guild_xp_canceled";
        public static string XpGuildXpReset => "xp_guild_xp_reset";
        public static string XpGuildLevelUp => "xp_guild_level_up";
        public static string XpGuildLevelUpRoleReward => "xp_guild_level_up_role_reward";
        
        public static string CommonYes => "common_yes";
        public static string CommonAnd => "common_and";
        public static string CommonOr => "common_or";
        public static string CommonReason => "common_reason";
        public static string CommonReasons => "common_reasons";
        public static string CommonContextType(string contextType) => $"common_{contextType}";
        public static string CommonUser => "common_user";
        public static string CommonUsers => "common_users";
        public static string CommonId => "common_id";
        public static string CommonTimesUp => "common_times_up";
        public static string CommonExample => "common_example";
        public static string CommonCooldown => "common_cooldown";
        public static string CommonCooldownBucketType(string bucketType) => $"common_{bucketType}";
        public static string CommonAmount => "common_amount";
        public static string CommonPeriod => "common_period";
        public static string CommonPer => "common_per";
        public static string CommonRank => "common_rank";
        public static string CommonGlobal => "common_global";
        public static string CommonServer => "common_server";
        public static string CommonMenuPage => "common_menu_page";
        public static string More => "common_more";
        
        public static string AttributeOwnerOnly => "attribute_owner_only";
        public static string AttributeUserGuildPermissions => "attribute_user_guild_permissions";
        public static string AttributeUserPermissionNotGuild => "attribute_user_permission_not_guild";
        public static string AttributeBotGuildPermissions => "attribute_bot_guild_permissions";
        public static string AttributeBotPermissionNotGuild => "attribute_bot_permission_not_guild";
        public static string AttributeContext => "attribute_context";
        
        public static string TypeParserPrimitiveType => "type_parser_primitive_type";
        public static string TypeParserCachedCategoryChannelNotGuild => "type_parser_cached_category_channel_not_guild";
        public static string TypeParserCachedMemberNotGuild => "type_parser_cached_member_not_guild";
        public static string TypeParserCachedRoleNotGuild => "type_parser_cached_role_not_guild";
        public static string TypeParserCachedTextChannelNotGuild => "type_parser_cached_text_channel_not_guild";
        public static string TypeParserCachedVoiceChannelNotGuild => "type_parser_cached_voice_channel_not_guild";
        public static string TypeParserTimeSpanUnsuccessful => "type_parser_time_span_unsuccessful";
        public static string TypeParserInvalidColor => "type_parser_invalid_color";
        
        public static string ServiceCommandNotExecuted => "service_command_not_executed";
        public static string ServiceCommandCooldown => "service_command_cooldown";
        public static string ServiceCommandLessArguments => "service_command_less_arguments";
        public static string ServiceCommandManyArguments => "service_command_many_arguments";
    }
}