using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IPTS.Data.Configurations
{
    public class MedicalCaseTestPhotoConfiguration : IEntityTypeConfiguration<MedicalCaseTestPhoto>
    {
        public void Configure(EntityTypeBuilder<MedicalCaseTestPhoto> builder)
        {
            builder.ToTable("MedicalCaseTestPhotos");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.PhotoKind)
                   .IsRequired()
                   .HasComment("0: Initial, 1: Final");

            builder.Property(p => p.Slot)
                   .IsRequired();

            builder.Property(p => p.FileName)
                   .IsRequired()
                   .HasMaxLength(260);

            builder.Property(p => p.OriginalFileName)
                   .IsRequired()
                   .HasMaxLength(260);

            builder.HasIndex(p => new { p.MedicalCaseId, p.TestId, p.PhotoKind, p.Slot })
                   .IsUnique();

            builder.HasOne(p => p.MedicalCase)
                   .WithMany(mc => mc.TestPhotos)
                   .HasForeignKey(p => p.MedicalCaseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Test)
                   .WithMany(t => t.TestPhotos)
                   .HasForeignKey(p => p.TestId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
