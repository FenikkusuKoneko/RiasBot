using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Rias.Database.Entities;

public class CharacterEntity : DbEntity, ICharacterEntity
{
    public int CharacterId { get; set; }
        
    public string? Name { get; set; }
        
    public string? Url { get; set; }
        
    public string? ImageUrl { get; set; }
        
    public string? Description { get; set; }
        
    [AllowNull]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
}

public class CharacterEntityTypeConfiguration : IEntityTypeConfiguration<CharacterEntity>
{
    public void Configure(EntityTypeBuilder<CharacterEntity> builder)
    {
        builder.HasIndex(c => c.CharacterId).IsUnique();
    }
}