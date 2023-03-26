using System.Diagnostics.CodeAnalysis;
using Disqord;

namespace Rias.Database.Entities;

public class CustomWaifuEntity : DbEntity, IWaifuEntity
{
    public Snowflake UserId { get; set; }

    [NotNull]
    public string? Name { get; set; }

    [NotNull]
    public string? ImageUrl { get; set; }

    public bool IsSpecial { get; set; }
    public int Position { get; set; }
}