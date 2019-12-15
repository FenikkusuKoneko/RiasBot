using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Rias.Core.Commons
{
    public class PatreonPledgeData
    {
        [JsonProperty("patreon_user_id")]
        public int PatreonUserId { get; set; }
        
        [JsonProperty("patreon_user_name")]
        public string? PatreonUserName { get; set; }
        
        [JsonProperty("amount_cents")]
        public int AmountCents { get; set; }
        
        [JsonProperty("last_charge_date")]
        public DateTimeOffset? LastChargeDate { get; set; }
        
        [JsonProperty("last_charge_status")]
        public LastChargeStatus? LastChargeStatus { get; set; }
        
        [JsonProperty("patron_status")]
        public PatronStatus? PatronStatus { get; set; }
        
        [JsonProperty("tier")]
        public int Tier { get; set; }
        
        [JsonProperty("tier_amount_cents")]
        public int TierAmountCents { get; set; }
        
        [JsonProperty("discord_id")]
        public ulong DiscordId { get; set; }
    }
    
    public enum LastChargeStatus
    {
        [EnumMember(Value = "paid")]
        Paid,
        [EnumMember(Value = "declined")]
        Declined,
        [EnumMember(Value = "pending")]
        Pending,
        [EnumMember(Value = "refunded")]
        Refunded,
        [EnumMember(Value = "fraud")]
        Fraud,
        [EnumMember(Value = "other")]
        Other
    }

    public enum PatronStatus
    {
        [EnumMember(Value = "active_patron")]
        ActivePatron,
        [EnumMember(Value = "declined_patron")]
        DeclinedPatron,
        [EnumMember(Value = "former_patron")]
        FormerPatron
    }
}