using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Data.Configurations
{
    public class AppUserConfiguration
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            // تحديد العلاقة مع UserType
            builder.HasOne(u => u.UserType)
                   .WithMany(t => t.Users)
                   .HasForeignKey(u => u.UserTypeId)
                   .OnDelete(DeleteBehavior.Restrict); // أو DeleteBehavior.Cascade حسب الحاجة

            builder.Property(x => x.Status)
               .HasConversion<byte>();  // ✅ enum → byte بدلاً من int
                                        // جعل FirstName و LastName حقول مطلوبة مع طول أقصى
            builder.Property(u => u.FirstName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.LastName)
                   .IsRequired()
                   .HasMaxLength(100);
        }
    }
}
