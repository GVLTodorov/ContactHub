using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactHub.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddContacts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Contacts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Surname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: false),
                PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Iban = table.Column<string>(type: "TEXT", maxLength: 34, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Address_PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Address_Street = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Contacts", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Contacts_Surname_FirstName",
            table: "Contacts",
            columns: new[] { "Surname", "FirstName" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Contacts");
    }
}
