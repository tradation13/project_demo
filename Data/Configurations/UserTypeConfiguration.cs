using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IPTS.Data.Configurations
{
    public class UserTypeConfiguration
    {
        public void Configure(EntityTypeBuilder<UserType> builder)
        {
            builder.Property(u => u.Name)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(u => u.DefaultAction)
                   .HasMaxLength(50)
                   .IsRequired(false); 

            builder.Property(u => u.DefaultController)
                   .HasMaxLength(50)
                   .IsRequired(false);

            builder.Property(u => u.DefaultArea)
                   .HasMaxLength(50)
                   .IsRequired(false);

            builder.HasOne(u => u.Role) // To be more scalable convert it to HasMany and add a new window of default roles for each user type
               .WithMany() 
               .HasForeignKey(u => u.DefaultRoleId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}
