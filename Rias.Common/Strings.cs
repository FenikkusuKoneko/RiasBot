namespace Rias.Common;

public static class Strings
{
    public static class Administration
    {
        public const string GreetEnabled = "administration_greet_enabled";
        public const string GreetEnabledChannel = "administration_greet_enabled_channel";
        public const string GreetDisabled = "administration_greet_disabled";
        public const string GreetMessageSetToDefault = "administration_greet_message_set_to_default";
        public const string GreetMessageLengthLimit = "administration_greet_message_length_limit";
        public const string GreetMessageSet = "administration_greet_message_set";
        public const string ByeEnabled = "administration_bye_enabled";
        public const string ByeEnabledChannel = "administration_bye_enabled_channel";
        public const string ByeDisabled = "administration_bye_disabled";
        public const string ByeMessageSetToDefault = "administration_bye_message_set_to_default";
        public const string ByeMessageLengthLimit = "administration_bye_message_length_limit";
        public const string ByeMessageSet = "administration_bye_message_set";
        public const string DefaultGreetMessage = "administration_default_greet_message";
        public const string DefaultByeMessage = "administration_default_bye_message";
        public const string ModLogEnabled = "administration_mod_log_enabled";
        public const string ModLogEnabledChannel = "administration_mod_log_enabled_channel";
        public const string ModLogDisabled = "administration_mod_log_disabled";
        public const string InvalidCustomMessage = "administration_invalid_custom_message";
        public const string ChannelNameLengthLimit = "administration_channel_name_length_limit";
        public const string CategoryChannelCreated = "administration_category_channel_created";
        public const string CategoryChannelRenamed = "administration_category_channel_renamed";
        public const string CategoryChannelDeleted = "administration_category_channel_deleted";
        public const string TextChannelAddedToCategory = "administration_text_channel_added_to_category";
        public const string VoiceChannelAddedToCategory = "administration_voice_channel_added_to_category";
        public const string AuthorMissingChannelManagePermission = "administration_author_missing_channel_manage_permission";
        public const string BotMissingChannelManagePermission = "administration_bot_missing_channel_manage_permission";
    }

    public static class Help
    {
        public const string Title = "help_title";
        public const string Info = "help_info";
        public const string SupportServer = "help_support_server";
        public const string InviteMe = "help_invite_me";
        public const string Website = "help_website";
        public const string Donate = "help_donate";
        public const string Footer = "help_footer";
        public const string CommandNotFound = "help_command_not_found";
        public const string Module = "help_module";
        public const string OwnerOnly = "help_owner_only";
        public const string RequiredPermissions = "help_required_permissions";
        public const string RequiredPermissionsYou = "help_required_permissions_you";
        public const string RequiredPermissionsMe = "help_required_permissions_me";
        public const string HelpCooldown = "help_cooldown";
        public const string HelpCooldownUses = "help_cooldown_uses";
        public const string HelpCooldownWindow = "help_cooldown_window";
        public const string HelpCooldownScope = "help_cooldown_scope";
        public const string ModulesListTitle = "help_modules_list_title";
        public const string ModulesListFooter = "help_modules_list_footer";
        public const string ModuleNotFound = "help_module_not_found";
        public const string AllModuleCommands = "help_all_module_commands";
        public const string AllSubmoduleCommands = "help_all_submodule_commands";
        public const string CommandInfo = "help_command_info";
        public const string CommandInfoFooter = "help_command_info_footer";
        public const string AllCommands = "help_all_commands";
    }

    public static class Utility
    {
        public const string PrefixIs = "utility_prefix_is";
        public const string PrefixNameOrMention = "utility_prefix_name_or_mention";
    }

    public static class Service
    {
        public const string CommandCooldown = "service_command_cooldown";
        public const string CommandNotExecuted = "service_command_not_executed";
        public const string CommandNotExecutedFooter = "service_command_not_executed_footer";
        public const string CommandException = "service_command_exception";
    }

    public static class Attribute
    {
        public const string MissingAuthorPermissions = "attribute_missing_author_permissions";
        public const string MissingAuthorGuildPermissions = "attribute_missing_author_guild_permissions";
        public const string MissingAuthorChannelPermissions = "attribute_missing_author_channel_permissions";
        public const string MissingBotPermissions = "attribute_missing_bot_permissions";
        public const string MissingBotGuildPermissions = "attribute_missing_bot_guild_permissions";
        public const string MissingBotChannelPermissions = "attribute_missing_bot_channel_permissions";
    }

    public static class TypeParser
    {
        public static string ChannelNotFound(string? type = null) => type switch
        {
            null => "type_parser_channel_not_found",
            "message" => "type_parser_text_channel_not_found",
            _ => $"type_parser_{type}_channel_not_found"
        };
    }

    public const string And = "and";
    public const string Channel = "channel";
    public const string Examples = "examples";
    public const string Links = "links";
    public const string Member = "member";
    public const string NoDescription = "no_description";
    public const string Server = "server";
    public const string Usages = "usages";
    public const string User = "user";
}