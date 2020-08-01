using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Rias.Core.Models
{
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