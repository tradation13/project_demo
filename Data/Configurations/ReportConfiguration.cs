using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Data.Configurations
{
    public class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.ToTable("Reports");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Summary)
                   .HasMaxLength(1000);

            builder.Property(r => r.CreatedAt)
             .HasDefaultValueSql("NOW()");

        }
    }

}
