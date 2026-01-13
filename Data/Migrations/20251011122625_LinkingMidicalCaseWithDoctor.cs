using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkingMidicalCaseWithDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoctorId",
                table: "MedicalCases",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_DoctorId",
                table: "MedicalCases",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Doctors_DoctorId",
                table: "MedicalCases",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Doctors_DoctorId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_DoctorId",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "MedicalCases");
        }
    }
}
