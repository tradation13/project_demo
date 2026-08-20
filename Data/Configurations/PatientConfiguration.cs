using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(p => p.Id);

        // // تكوين IdentityNumber
        // builder.Property(p => p.IdentityNumber)
        //        .IsRequired()
        //        .HasMaxLength(50);

        builder.Property(p => p.BirthDate).IsRequired(false);

        builder.Property(p => p.Weight).IsRequired(false);
        builder.Property(p => p.Height).IsRequired(false);

        builder.Property(p => p.BloodGroup)
               .HasConversion<byte?>()
               .HasComment("0: A+, 1: A-, 2: B+, 3: B-, 4: O+, 5: O-, 6: AB+, 7: AB-")
               .IsRequired(false);

        builder.Property(p => p.IsSmoker).IsRequired(false);
        builder.Property(p => p.HasChronicDisease).IsRequired(false);

        // العلاقة مع AppUser
        builder.HasOne(p => p.User)
             .WithOne(u => u.Patient)
             .HasForeignKey<Patient>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

        // العلاقة مع Appointments
        builder.HasMany(p => p.Appointments)
               .WithOne(a => a.Patient)
               .HasForeignKey(a => a.PatientId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
