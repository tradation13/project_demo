using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IPTS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalCatalogAndTestParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StandardValue",
                table: "Tests",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MedicalConditionId",
                table: "MedicalCases",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicalConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalConditions", x => x.Id);
                });

            migrationBuilder.Sql(@"
INSERT INTO ""MedicalConditions"" (""Name"")
SELECT DISTINCT TRIM(""Name"")
FROM ""MedicalCases""
WHERE ""Name"" IS NOT NULL AND TRIM(""Name"") <> ''
AND NOT EXISTS (
    SELECT 1 FROM ""MedicalConditions"" mc WHERE mc.""Name"" = TRIM(""MedicalCases"".""Name"")
);

UPDATE ""MedicalCases"" c
SET ""MedicalConditionId"" = mc.""Id""
FROM ""MedicalConditions"" mc
WHERE TRIM(c.""Name"") = mc.""Name""
  AND c.""MedicalConditionId"" IS NULL;
");

            migrationBuilder.CreateTable(
                name: "TestParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TestId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestParameters_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_MedicalConditionId",
                table: "MedicalCases",
                column: "MedicalConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestParameters_TestId",
                table: "TestParameters",
                column: "TestId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_MedicalConditions_MedicalConditionId",
                table: "MedicalCases",
                column: "MedicalConditionId",
                principalTable: "MedicalConditions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_MedicalConditions_MedicalConditionId",
                table: "MedicalCases");

            migrationBuilder.DropTable(
                name: "MedicalConditions");

            migrationBuilder.DropTable(
                name: "TestParameters");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_MedicalConditionId",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "StandardValue",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "MedicalConditionId",
                table: "MedicalCases");
        }
    }
}
