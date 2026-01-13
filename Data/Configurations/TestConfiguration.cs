using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

public class TestConfiguration : IEntityTypeConfiguration<Test>
{
    public void Configure(EntityTypeBuilder<Test> builder)
    {
        builder.ToTable("Tests");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasOne(t => t.TestGroup)
               .WithMany(tg => tg.Tests)
               .HasForeignKey(t => t.TestGroupId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}