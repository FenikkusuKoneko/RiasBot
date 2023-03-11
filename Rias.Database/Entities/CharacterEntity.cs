using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Rias.Database.Entities;

public class CharacterEntity : DbEntity, ICharacterEntity
{
    public int CharacterId { get; set; }
    
    [NotNull]
    public string? Name { get; set; }
    
    [NotNull]
    public string? Url { get; set; }
    
    [NotNull]
    public string? ImageUrl { get; set; }
    
    public string? Description { get; set; }
    
    [NotNull]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector? SearchVector { get; set; }
}

public class CharacterEntityTypeConfiguration : IEntityTypeConfiguration<CharacterEntity>
{
    public void Configure(EntityTypeBuilder<CharacterEntity> builder)
    {
        builder.HasIndex(c => c.CharacterId).IsUnique();
        builder.HasGeneratedTsVectorColumn(
                c => c.SearchVector,
                "english",
                c => new { c.Name, c.Description })
            .HasIndex(c => c.SearchVector)
            .HasMethod("GIN");
    }
}