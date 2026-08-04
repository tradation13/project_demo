using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Data.Configurations
{
    public class MedicalCaseTestConfiguration : IEntityTypeConfiguration<MedicalCaseTest>
    {
        public void Configure(EntityTypeBuilder<MedicalCaseTest> builder)
        {
            builder.ToTable("MedicalCaseTests");
            builder.HasKey(mct => mct.Id);

            builder.Property(mct => mct.Result)
                   .HasMaxLength(500);

            builder.Property(mct => mct.StandardValue)
                   .HasColumnType("numeric(18,4)")
                   .IsRequired(false);

            builder.HasOne(mct => mct.MedicalCase)
                   .WithMany(mc => mc.MedicalCaseTests)
                   .HasForeignKey(mct => mct.MedicalCaseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(mct => mct.Test)
                   .WithMany(t => t.MedicalCaseTests)
                   .HasForeignKey(mct => mct.TestId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
