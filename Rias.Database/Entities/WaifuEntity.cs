using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rias.Database.Entities;

public class WaifuEntity : DbEntity, IWaifuEntity
{
    public Snowflake UserId { get; set; }
    public int? CharacterId { get; set; }
    public int? CustomCharacterId { get; set; }
    public string Name => (Character?.Name ?? CustomCharacter?.Name) ?? string.Empty;
    public string ImageUrl => (Character?.ImageUrl ?? CustomCharacter?.ImageUrl) ?? string.Empty;
    public string? CustomImageUrl { get; set; }
    public int Price { get; set; }
    public bool IsSpecial { get; set; }
    public int Position { get; set; }

    public CharacterEntity? Character { get; set; }
    public CustomCharacterEntity? CustomCharacter { get; set; }
}

public class WaifuEntityTypeConfiguration : IEntityTypeConfiguration<WaifuEntity>
{
    public void Configure(EntityTypeBuilder<WaifuEntity> builder)
    {
        builder.HasOne(w => w.Character)
            .WithMany()
            .HasForeignKey(w => w.CharacterId)
            .HasPrincipalKey(w => w.CharacterId);

        builder.HasOne(w => w.CustomCharacter)
            .WithMany()
            .HasForeignKey(w => w.CustomCharacterId)
            .HasPrincipalKey(w => w.CharacterId);
    }
}