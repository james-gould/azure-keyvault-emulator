using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzureKeyVaultEmulator.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Migration_b108255a : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificateContacts",
                columns: table => new
                {
                    PersistedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersistedVersion = table.Column<string>(type: "TEXT", nullable: false),
                    PersistedName = table.Column<string>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    BackingContacts = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateContacts", x => x.PersistedId);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    PersistedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersistedName = table.Column<string>(type: "TEXT", nullable: false),
                    PersistedVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CertificateContents = table.Column<string>(type: "TEXT", nullable: false),
                    KeyId = table.Column<string>(type: "TEXT", nullable: false),
                    SecretId = table.Column<string>(type: "TEXT", nullable: false),
                    CertificateBlob = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CertificateIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    RecoveryId = table.Column<string>(type: "TEXT", nullable: false),
                    CertificateName = table.Column<string>(type: "TEXT", nullable: false),
                    VaultUri = table.Column<string>(type: "TEXT", nullable: false),
                    X509Thumbprint = table.Column<string>(type: "TEXT", nullable: false),
                    Attributes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.PersistedId);
                });

            migrationBuilder.CreateTable(
                name: "Issuers",
                columns: table => new
                {
                    PersistedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersistedName = table.Column<string>(type: "TEXT", nullable: false),
                    PersistedVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    IssuerName = table.Column<string>(type: "TEXT", nullable: false),
                    Attributes = table.Column<string>(type: "TEXT", nullable: false),
                    Credentials = table.Column<string>(type: "TEXT", nullable: false),
                    OrganisationDetails = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issuers", x => x.PersistedId);
                });

            migrationBuilder.CreateTable(
                name: "Keys",
                columns: table => new
                {
                    PersistedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersistedName = table.Column<string>(type: "TEXT", nullable: false),
                    PersistedVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Managed = table.Column<bool>(type: "INTEGER", nullable: true),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Attributes = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keys", x => x.PersistedId);
                });

            migrationBuilder.CreateTable(
                name: "Secrets",
                columns: table => new
                {
                    PersistedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersistedName = table.Column<string>(type: "TEXT", nullable: false),
                    PersistedVersion = table.Column<string>(type: "TEXT", nullable: false),
                    SecretIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Managed = table.Column<bool>(type: "INTEGER", nullable: true),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Attributes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secrets", x => x.PersistedId);
                });

            migrationBuilder.CreateTable(
                name: "CertificatePolicies",
                columns: table => new
                {
                    PersistedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersistedName = table.Column<string>(type: "TEXT", nullable: false),
                    PersistedVersion = table.Column<string>(type: "TEXT", nullable: false),
                    ParentCertificateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IssuerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Identifier = table.Column<string>(type: "TEXT", nullable: false),
                    IssuerPersistedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CertificateProperties_BackingEnhancedUsage = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateProperties_BackingKeyUsage = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateProperties_SubjectAlternativeNames_BackingDns = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateProperties_SubjectAlternativeNames_BackingEmails = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateProperties_SubjectAlternativeNames_BackingUpns = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateProperties_Subject = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateProperties_ValidityMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    BackingLifetimeActions = table.Column<string>(type: "TEXT", nullable: false),
                    CertificateAttributes = table.Column<string>(type: "TEXT", nullable: false),
                    KeyProperties = table.Column<string>(type: "TEXT", nullable: true),
                    SecretProperies = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificatePolicies", x => x.PersistedId);
                    table.ForeignKey(
                        name: "FK_CertificatePolicies_Certificates_ParentCertificateId",
                        column: x => x.ParentCertificateId,
                        principalTable: "Certificates",
                        principalColumn: "PersistedId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificatePolicies_Issuers_IssuerId",
                        column: x => x.IssuerId,
                        principalTable: "Issuers",
                        principalColumn: "PersistedId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CertificatePolicies_Issuers_IssuerPersistedId",
                        column: x => x.IssuerPersistedId,
                        principalTable: "Issuers",
                        principalColumn: "PersistedId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificatePolicies_IssuerId",
                table: "CertificatePolicies",
                column: "IssuerId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificatePolicies_IssuerPersistedId",
                table: "CertificatePolicies",
                column: "IssuerPersistedId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificatePolicies_ParentCertificateId",
                table: "CertificatePolicies",
                column: "ParentCertificateId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateContacts");

            migrationBuilder.DropTable(
                name: "CertificatePolicies");

            migrationBuilder.DropTable(
                name: "Keys");

            migrationBuilder.DropTable(
                name: "Secrets");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "Issuers");
        }
    }
}
