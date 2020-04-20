namespace Rias.Core.Database.Entities
{
    public class GuildsEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        public string? Prefix { get; set; }
        public ulong MuteRoleId { get; set; }
        public bool GreetNotification { get; set; }
        public string? GreetMessage { get; set; }
        public ulong GreetWebhookId { get; set; }
        public bool ByeNotification { get; set; }
        public string? ByeMessage { get; set; }
        public ulong ByeWebhookId { get; set; }
        public bool GuildXpNotification { get; set; }
        public ulong AutoAssignableRoleId { get; set; }
        public int PunishmentWarningsRequired { get; set; }
        public string? WarningPunishment { get; set; }
        public ulong ModLogChannelId { get; set; }
        public bool DeleteCommandMessage { get; set; }
        public string? Locale { get; set; }
    }
}