using System.Diagnostics.CodeAnalysis;
using NpgsqlTypes;

namespace Rias.Database.Entities;

public interface ICharacterEntity
{
    public int CharacterId { get; set; }

    [NotNull]
    public string? Name { get; set; }

    [NotNull]
    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public NpgsqlTsVector SearchVector { get; set; }
}