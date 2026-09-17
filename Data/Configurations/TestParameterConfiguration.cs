using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IPTS.Data.Configurations
{
    public class TestParameterConfiguration : IEntityTypeConfiguration<TestParameter>
    {
        public void Configure(EntityTypeBuilder<TestParameter> builder)
        {
            builder.ToTable("TestParameters");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Key)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(p => p.Value)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(p => p.Date)
                   .IsRequired(false);

            builder.HasOne(p => p.Test)
                   .WithMany(t => t.Parameters)
                   .HasForeignKey(p => p.TestId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
