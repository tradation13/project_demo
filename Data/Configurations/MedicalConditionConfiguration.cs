using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IPTS.Data.Configurations
{
    public class MedicalConditionConfiguration : IEntityTypeConfiguration<MedicalCondition>
    {
        public void Configure(EntityTypeBuilder<MedicalCondition> builder)
        {
            builder.ToTable("MedicalConditions");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                   .IsRequired()
                   .HasMaxLength(150);
        }
    }
}
