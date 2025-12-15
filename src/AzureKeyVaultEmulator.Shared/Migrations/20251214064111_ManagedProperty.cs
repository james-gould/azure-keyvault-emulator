using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzureKeyVaultEmulator.Shared.Migrations
{
    /// <inheritdoc />
    public partial class ManagedProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Managed",
                table: "Secrets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Managed",
                table: "Keys",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Managed",
                table: "Secrets");

            migrationBuilder.DropColumn(
                name: "Managed",
                table: "Keys");
        }
    }
}
