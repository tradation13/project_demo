using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

public class TestGroupConfiguration : IEntityTypeConfiguration<TestGroup>
{
    public void Configure(EntityTypeBuilder<TestGroup> builder)
    {
        builder.ToTable("TestGroups");
        builder.HasKey(tg => tg.Id);
        builder.Property(tg => tg.Name)
               .IsRequired()
               .HasMaxLength(100);
    }
}


