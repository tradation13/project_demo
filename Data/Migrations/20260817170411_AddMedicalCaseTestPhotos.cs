using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IPTS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalCaseTestPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicalCaseTestPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedicalCaseId = table.Column<int>(type: "integer", nullable: false),
                    TestId = table.Column<int>(type: "integer", nullable: false),
                    PhotoKind = table.Column<int>(type: "integer", nullable: false, comment: "0: Initial, 1: Final"),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCaseTestPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalCaseTestPhotos_MedicalCases_MedicalCaseId",
                        column: x => x.MedicalCaseId,
                        principalTable: "MedicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalCaseTestPhotos_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCaseTestPhotos_MedicalCaseId_TestId_PhotoKind_Slot",
                table: "MedicalCaseTestPhotos",
                columns: new[] { "MedicalCaseId", "TestId", "PhotoKind", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCaseTestPhotos_TestId",
                table: "MedicalCaseTestPhotos",
                column: "TestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalCaseTestPhotos");
        }
    }
}
