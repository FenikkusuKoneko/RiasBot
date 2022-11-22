using Disqord;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Rias.Database;

public class SnowflakeConverter : ValueConverter<Snowflake, ulong>
{
    public SnowflakeConverter() : base(v => v.RawValue, v => new Snowflake(v))
    {
    }
}