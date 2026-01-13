using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ScheduledTime)
               .IsRequired();

        builder.Property(a => a.Notes)
               .IsRequired(false)
               .HasDefaultValue(string.Empty);

        builder.Property(a => a.Status)
               .HasDefaultValue(AppointmentStatus.Pending);
               
        builder.Property(a => a.StartSlotIndex)
               .IsRequired();
               
        builder.Property(a => a.EndSlotIndex)
               .IsRequired();

        builder.HasOne(a => a.Patient)
               .WithMany(p => p.Appointments)
               .HasForeignKey(a => a.PatientId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Doctor)
               .WithMany(d => d.Appointments)
               .HasForeignKey(a => a.DoctorId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
