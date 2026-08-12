using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IPTS.Data.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.HasOne(u => u.UserType)
                   .WithMany(t => t.Users)
                   .HasForeignKey(u => u.UserTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Status)
               .HasConversion<byte>();

            builder.Property(u => u.FirstName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.LastName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.AcceptedPrivacyPolicy)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(u => u.AcceptedTermsOfUse)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(u => u.ChatHistoryEnabled)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(u => u.AcceptedHealthDataConsent)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(u => u.HealthDataConsentAcceptedAt)
                   .IsRequired(false);
        }
    }
}
