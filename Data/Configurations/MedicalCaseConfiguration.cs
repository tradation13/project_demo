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
