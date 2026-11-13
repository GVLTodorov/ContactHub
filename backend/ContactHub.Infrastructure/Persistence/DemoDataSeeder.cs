using ContactHub.Domain.Entities;
using ContactHub.Domain.ValueObjects;

namespace ContactHub.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public static void Seed(AppDbContext context, TimeProvider timeProvider)
    {
        if (context.Contacts.Any())
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var contacts = new[]
        {
            Build("Ada", "Lovelace", new DateOnly(1990, 4, 12), ("Main St 1", "10115", "Berlin", "Germany"), "+49 30 123456", "DE89 3704 0044 0532 0130 00", now),
            Build("Grace", "Hopper", new DateOnly(1985, 12, 9), ("221B Baker Street", "NW1 6XE", "London", "United Kingdom"), "+44 20 7946 0958", "GB82 WEST 1234 5698 7654 32", now.AddMinutes(1)),
            Build("Mateo", "García", new DateOnly(1992, 7, 23), ("Calle de Alcalá 45", "28014", "Madrid", "Spain"), "+34 912 345 678", "ES91 2100 0418 4502 0005 1332", now.AddMinutes(2)),
            Build("Elena", "Petrova", new DateOnly(1988, 3, 5), ("bul. Vitosha 100", "1000", "Sofia", "Bulgaria"), "+359 88 812 3456", "BG80 BNBG 9661 1020 3456 78", now.AddMinutes(3)),
            Build("Lucas", "Dubois", new DateOnly(1979, 10, 30), ("12 Rue de Rivoli", "75004", "Paris", "France"), "+33 1 42 68 53 00", "FR14 2004 1010 0505 0001 3M02 606", now.AddMinutes(4)),
        };

        context.Contacts.AddRange(contacts);
        context.SaveChanges();
    }

    private static Contact Build(
        string firstName,
        string surname,
        DateOnly dateOfBirth,
        (string Street, string PostalCode, string City, string Country) address,
        string phone,
        string iban,
        DateTime now) =>
        Contact.Create(
            firstName,
            surname,
            dateOfBirth,
            Address.Create(address.Street, address.PostalCode, address.City, address.Country),
            PhoneNumber.Create(phone),
            Iban.Create(iban),
            now);
}
