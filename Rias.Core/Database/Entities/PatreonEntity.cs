using System;
using Rias.Core.Commons;

namespace Rias.Core.Database.Entities
{
    public class PatreonEntity : DbEntity
    {
        public ulong UserId { get; set; }
        public int PatreonUserId { get; set; }
        public string? PatreonUserName { get; set; }
        public int AmountCents { get; set; }
        public DateTimeOffset? LastChargeDate { get; set; }
        public LastChargeStatus? LastChargeStatus { get; set; }
        public PatronStatus? PatronStatus { get; set; }
        public int Tier { get; set; }
        public int TierAmountCents { get; set; }
        public bool Checked { get; set; }
    }
}