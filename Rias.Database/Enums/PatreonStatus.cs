using System.Runtime.Serialization;

namespace Rias.Database.Enums;

public enum PatronStatus
{
    [EnumMember(Value = "active_patron")]
    ActivePatron,

    [EnumMember(Value = "declined_patron")]
    DeclinedPatron,

    [EnumMember(Value = "former_patron")]
    FormerPatron
}