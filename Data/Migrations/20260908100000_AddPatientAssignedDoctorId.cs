using IPTS.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260908100000_AddPatientAssignedDoctorId")]
    public partial class AddPatientAssignedDoctorId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedDoctorId",
                table: "Patients",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_AssignedDoctorId",
                table: "Patients",
                column: "AssignedDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Doctors_AssignedDoctorId",
                table: "Patients",
                column: "AssignedDoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Doctors_AssignedDoctorId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_AssignedDoctorId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "AssignedDoctorId",
                table: "Patients");
        }
    }
}
