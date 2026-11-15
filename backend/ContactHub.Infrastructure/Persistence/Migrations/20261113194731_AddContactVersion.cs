using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactHub.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddContactVersion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Version",
            table: "Contacts",
            type: "INTEGER",
            nullable: false,
            // Existing rows start at 1, like newly created contacts.
            defaultValue: 1);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Version",
            table: "Contacts");
    }
}
