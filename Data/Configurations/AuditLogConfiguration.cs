using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IPTS.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Action).IsRequired();
            builder.Property(a => a.Details).IsRequired().HasMaxLength(2000);
            builder.Property(a => a.ActorUserId).HasMaxLength(450);
            builder.Property(a => a.ActorUserName).HasMaxLength(256);
            builder.Property(a => a.TargetUserId).HasMaxLength(450);
            builder.Property(a => a.EntityName).HasMaxLength(100);
            builder.Property(a => a.EntityId).HasMaxLength(100);
            builder.Property(a => a.IpAddress).HasMaxLength(64);
            builder.Property(a => a.CreatedAt).IsRequired();

            builder.HasIndex(a => a.CreatedAt);
            builder.HasIndex(a => a.Action);
            builder.HasIndex(a => a.ActorUserId);

            builder.Ignore(a => a.ActionEnum);
        }
    }
}
