using System.Runtime.Serialization;

namespace Rias.Database.Enums;

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