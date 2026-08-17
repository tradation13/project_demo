using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Data.Configurations
{
    public class MedicalCaseConfiguration : IEntityTypeConfiguration<MedicalCase>
    {
        public void Configure(EntityTypeBuilder<MedicalCase> builder)
        {
            builder.ToTable("MedicalCases");
            builder.HasKey(mc => mc.Id);

            builder.Property(mc => mc.Name)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(mc => mc.Description)
                   .IsRequired()
                   .HasMaxLength(2000);

            builder.Property(mc => mc.DominantSide)
                   .HasConversion<byte?>()
                   .HasComment("0: RightSide, 1: LeftSide")
                   .IsRequired(false);

            builder.Property(mc => mc.ActivityLevel)
                   .HasConversion<byte?>()
                   .HasComment("0: Sedentary, 1: Moderate, 2: Active, 3: Professional")
                   .IsRequired(false);

            // --- إعدادات الحقول الإضافية ---
            builder.Property(mc => mc.InjuryHistory)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            builder.Property(mc => mc.Medications)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            builder.Property(mc => mc.FunctionalAbility)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            builder.Property(mc => mc.PersonalGoals)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            builder.HasOne(mc => mc.Patient)
                   .WithMany(a => a.MedicalCases)
                   .HasForeignKey(mc => mc.PatientId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(mc => mc.Doctor)
                 .WithMany(a => a.MedicalCases)
                 .HasForeignKey(mc => mc.DoctorId)
                 .OnDelete(DeleteBehavior.Cascade);
        }
    }

   
}
