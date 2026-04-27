using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IPTS.Data.Configurations
{
    public class MedicalReportHistoryConfiguration : IEntityTypeConfiguration<MedicalReportHistory>
    {
        public void Configure(EntityTypeBuilder<MedicalReportHistory> builder)
        {
            builder.ToTable("MedicalReportHistories");
            builder.HasKey(mrh => mrh.Id);

            builder.Property(mrh => mrh.ReportUrl)
                   .IsRequired();

            builder.Property(mrh => mrh.CreatedAt)
                   .IsRequired();

            builder.HasOne(mrh => mrh.User)
                   .WithMany()
                   .HasForeignKey(mrh => mrh.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(mrh => mrh.MedicalCase)
                   .WithMany(mc => mc.MedicalReportHistories)
                   .HasForeignKey(mrh => mrh.MedicalCaseId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
