using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Registry.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "registry");

            migrationBuilder.CreateTable(
                name: "Publisher",
                schema: "registry",
                columns: table => new
                {
                    PublisherId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValueSql: "SUSER_SNAME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publisher", x => x.PublisherId);
                    table.CheckConstraint("CK_Publisher_ContactEmail", "[ContactEmail] LIKE N'%_@_%._%'");
                    table.CheckConstraint("CK_Publisher_Slug_Format", "LEN([Slug]) >= 2 AND [Slug] NOT LIKE N'%[^a-z0-9-]%'");
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                schema: "registry",
                columns: table => new
                {
                    TagId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValueSql: "SUSER_SNAME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.TagId);
                    table.CheckConstraint("CK_Tag_Name_Format", "LEN([Name]) >= 2 AND [Name] NOT LIKE N'%[^a-z0-9-]%'");
                });

            migrationBuilder.CreateTable(
                name: "Package",
                schema: "registry",
                columns: table => new
                {
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublisherId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProjectUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    LicensePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    DownloadCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    IsListed = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValueSql: "SUSER_SNAME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Package", x => x.PackageId);
                    table.CheckConstraint("CK_Package_DownloadCount_NonNegative", "[DownloadCount] >= 0");
                    table.CheckConstraint("CK_Package_LicensePrice_NonNegative", "[LicensePrice] >= 0");
                    table.CheckConstraint("CK_Package_Name_Format", "LEN([Name]) >= 2 AND [Name] NOT LIKE N'%[^a-zA-Z0-9._-]%'");
                    table.CheckConstraint("CK_Package_ProjectUrl_Scheme", "[ProjectUrl] IS NULL OR [ProjectUrl] LIKE N'https://%'");
                    table.ForeignKey(
                        name: "FK_Package_Publisher",
                        column: x => x.PublisherId,
                        principalSchema: "registry",
                        principalTable: "Publisher",
                        principalColumn: "PublisherId");
                });

            migrationBuilder.CreateTable(
                name: "PackageTag",
                schema: "registry",
                columns: table => new
                {
                    PackageId = table.Column<long>(type: "bigint", nullable: false),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValueSql: "SUSER_SNAME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageTag", x => new { x.PackageId, x.TagId });
                    table.ForeignKey(
                        name: "FK_PackageTag_Package",
                        column: x => x.PackageId,
                        principalSchema: "registry",
                        principalTable: "Package",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageTag_Tag",
                        column: x => x.TagId,
                        principalSchema: "registry",
                        principalTable: "Tag",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageVersion",
                schema: "registry",
                columns: table => new
                {
                    PackageVersionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageId = table.Column<long>(type: "bigint", nullable: false),
                    SemVer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MajorVersion = table.Column<int>(type: "int", nullable: false),
                    MinorVersion = table.Column<int>(type: "int", nullable: false),
                    PatchVersion = table.Column<int>(type: "int", nullable: false),
                    PreReleaseTag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValueSql: "SUSER_SNAME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageVersion", x => x.PackageVersionId);
                    table.CheckConstraint("CK_PackageVersion_Numbers_NonNegative", "[MajorVersion] >= 0 AND [MinorVersion] >= 0 AND [PatchVersion] >= 0");
                    table.CheckConstraint("CK_PackageVersion_SemVer_MatchesParts", "[SemVer] = CONCAT([MajorVersion], N'.', [MinorVersion], N'.', [PatchVersion], CASE WHEN [PreReleaseTag] IS NULL THEN N'' ELSE CONCAT(N'-', [PreReleaseTag]) END)");
                    table.CheckConstraint("CK_PackageVersion_Sha256_Hex", "LEN([Sha256]) = 64 AND [Sha256] NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_PackageVersion_SizeBytes_Positive", "[SizeBytes] > 0");
                    table.ForeignKey(
                        name: "FK_PackageVersion_Package",
                        column: x => x.PackageId,
                        principalSchema: "registry",
                        principalTable: "Package",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageManifest",
                schema: "registry",
                columns: table => new
                {
                    PackageVersionId = table.Column<long>(type: "bigint", nullable: false),
                    ReadmeMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseExpression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetFramework = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntryPoints = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValueSql: "SUSER_SNAME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageManifest", x => x.PackageVersionId);
                    table.CheckConstraint("CK_PackageManifest_EntryPoints_Json", "[EntryPoints] IS NULL OR ISJSON([EntryPoints]) = 1");
                    table.CheckConstraint("CK_PackageManifest_TargetFramework", "[TargetFramework] LIKE N'net[0-9]%'");
                    table.ForeignKey(
                        name: "FK_PackageManifest_PackageVersion",
                        column: x => x.PackageVersionId,
                        principalSchema: "registry",
                        principalTable: "PackageVersion",
                        principalColumn: "PackageVersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "registry",
                table: "Publisher",
                columns: new[] { "PublisherId", "ContactEmail", "CreatedAt", "CreatedBy", "DisplayName", "IsVerified", "Slug", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, "platform@devhub.local", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Platform Engineering", true, "platform-team", null, null },
                    { 2L, "payments@devhub.local", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Payments Engineering", true, "payments-team", null, null }
                });

            migrationBuilder.InsertData(
                schema: "registry",
                table: "Publisher",
                columns: new[] { "PublisherId", "ContactEmail", "CreatedAt", "CreatedBy", "DisplayName", "Slug", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 3L, "research@devhub.local", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Innovation Research", "research-team", null, null });

            migrationBuilder.InsertData(
                schema: "registry",
                table: "Tag",
                columns: new[] { "TagId", "CreatedAt", "CreatedBy", "Description", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Identity, tokens and access control", "authentication", null, null },
                    { 2L, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Blob, file and object storage", "storage", null, null },
                    { 3L, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Billing, invoicing and settlement", "payments", null, null },
                    { 4L, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Cross-cutting platform concerns", "infrastructure", null, null },
                    { 5L, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Not recommended for production use", "experimental", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Package_DownloadCount",
                schema: "registry",
                table: "Package",
                column: "DownloadCount",
                descending: new bool[0],
                filter: "[IsDeleted] = 0 AND [IsListed] = 1")
                .Annotation("SqlServer:Include", new[] { "Name", "PublisherId" });

            migrationBuilder.CreateIndex(
                name: "IX_Package_PublisherId_Name",
                schema: "registry",
                table: "Package",
                columns: new[] { "PublisherId", "Name" },
                filter: "[IsDeleted] = 0 AND [IsListed] = 1")
                .Annotation("SqlServer:Include", new[] { "Description", "DownloadCount" });

            migrationBuilder.CreateIndex(
                name: "UQ_Package_Publisher_Name",
                schema: "registry",
                table: "Package",
                columns: new[] { "PublisherId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageTag_TagId_AssignedAt",
                schema: "registry",
                table: "PackageTag",
                columns: new[] { "TagId", "AssignedAt" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "PackageId" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersion_Ordering",
                schema: "registry",
                table: "PackageVersion",
                columns: new[] { "PackageId", "MajorVersion", "MinorVersion", "PatchVersion" },
                descending: new[] { false, true, true, true },
                filter: "[IsDeleted] = 0 AND [IsDeprecated] = 0")
                .Annotation("SqlServer:Include", new[] { "SemVer", "PublishedAt", "SizeBytes" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersion_Package_PublishedAt",
                schema: "registry",
                table: "PackageVersion",
                columns: new[] { "PackageId", "PublishedAt" },
                descending: new[] { false, true },
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "SemVer", "SizeBytes", "IsDeprecated" });

            migrationBuilder.CreateIndex(
                name: "UQ_PackageVersion_Package_SemVer",
                schema: "registry",
                table: "PackageVersion",
                columns: new[] { "PackageId", "SemVer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_PackageVersion_Sha256",
                schema: "registry",
                table: "PackageVersion",
                column: "Sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Publisher_ContactEmail",
                schema: "registry",
                table: "Publisher",
                column: "ContactEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Publisher_Slug",
                schema: "registry",
                table: "Publisher",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Tag_Name",
                schema: "registry",
                table: "Tag",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageManifest",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "PackageTag",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "PackageVersion",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "Tag",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "Package",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "Publisher",
                schema: "registry");
        }
    }
}
