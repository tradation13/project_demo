using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IPTS.Data.Configurations
{
    public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
    {
        public void Configure(EntityTypeBuilder<ChatConversation> builder)
        {
            builder.ToTable("ChatConversations");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.SessionId)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(c => c.UserId)
                   .HasMaxLength(450);

            builder.Property(c => c.UserType)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(c => c.IpAddress)
                   .HasMaxLength(64);

            builder.Property(c => c.ConsentGiven)
                   .IsRequired();

            builder.Property(c => c.ConsentDate)
                   .IsRequired(false);

            builder.Property(c => c.CreatedAt)
                   .IsRequired();

            builder.Property(c => c.LastMessageAt)
                   .IsRequired();

            builder.HasIndex(c => c.SessionId)
                   .IsUnique();

            builder.HasIndex(c => c.UserId);
            builder.HasIndex(c => c.LastMessageAt);

            builder.HasMany(c => c.Messages)
                   .WithOne(m => m.ChatConversation)
                   .HasForeignKey(m => m.ChatConversationId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
