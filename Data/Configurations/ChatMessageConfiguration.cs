using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IPTS.Data.Configurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.ChatConversationId)
                   .IsRequired();

            builder.Property(m => m.SenderType)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(m => m.Message)
                   .IsRequired()
                   .HasMaxLength(4000);

            builder.Property(m => m.CreatedAt)
                   .IsRequired();

            builder.HasIndex(m => m.ChatConversationId);
            builder.HasIndex(m => m.CreatedAt);
        }
    }
}
