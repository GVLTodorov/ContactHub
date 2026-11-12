using Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(Contact.MaxNameLength);
        builder.Property(c => c.Surname).IsRequired().HasMaxLength(Contact.MaxNameLength);
        builder.Property(c => c.DateOfBirth).HasColumnType("TEXT");

        builder.ComplexProperty(c => c.Address, a =>
        {
            a.Property(x => x.Street).IsRequired().HasMaxLength(Address.MaxStreet);
            a.Property(x => x.PostalCode).IsRequired().HasMaxLength(Address.MaxPostalCode);
            a.Property(x => x.City).IsRequired().HasMaxLength(Address.MaxCity);
            a.Property(x => x.Country).IsRequired().HasMaxLength(Address.MaxCountry);
        });

        builder.Property(c => c.PhoneNumber)
            .HasConversion(v => v.Value, s => PhoneNumber.Create(s))
            .IsRequired()
            .HasMaxLength(20);
        builder.Property(c => c.Iban)
            .HasConversion(v => v.Value, s => Iban.Create(s))
            .IsRequired()
            .HasMaxLength(34);

        builder.Property(c => c.CreatedAt).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        builder.Property(c => c.UpdatedAt).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.HasIndex(c => new { c.Surname, c.FirstName });
    }
}
