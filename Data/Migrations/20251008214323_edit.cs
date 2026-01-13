using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Data.Migrations
{
    /// <inheritdoc />
    public partial class edit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Appointments_AppointmentId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_AppointmentId",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "MedicalCases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "MedicalCases",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_AppointmentId",
                table: "MedicalCases",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Appointments_AppointmentId",
                table: "MedicalCases",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }
    }
}
