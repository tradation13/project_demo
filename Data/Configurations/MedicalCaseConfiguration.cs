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

                   // --- إعدادات الحقول الجديدة (Physical Vitals) ---

            // Weight & Height
            builder.Property(mc => mc.Weight).IsRequired(false);
            builder.Property(mc => mc.Height).IsRequired(false);

            // Blood Group
            builder.Property(mc => mc.BloodGroup)
                   .HasConversion<byte?>()
                   .HasComment("0: A+, 1: A-, 2: B+, 3: B-, 4: O+, 5: O-, 6: AB+, 7: AB-")
                   .IsRequired(false);

            // Dominant Side
            builder.Property(mc => mc.DominantSide)
                   .HasConversion<byte?>()
                   .HasComment("0: RightSide, 1: LeftSide")
                   .IsRequired(false);

            // Activity Level
            builder.Property(mc => mc.ActivityLevel)
                   .HasConversion<byte?>()
                   .HasComment("0: Sedentary, 1: Moderate, 2: Active, 3: Professional")
                   .IsRequired(false);

            // Boolean Fields
            builder.Property(mc => mc.IsSmoker).IsRequired(false);
            builder.Property(mc => mc.HasChronicDisease).IsRequired(false);

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
