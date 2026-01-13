using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Data.Migrations
{
    /// <inheritdoc />
    public partial class modfiymidicalstatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Appointments_AppointmentId",
                table: "MedicalCases");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MedicalCaseTests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "AppointmentId",
                table: "MedicalCases",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MedicalCases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "PatientId",
                table: "MedicalCases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PatientId",
                table: "MedicalCases",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Appointments_AppointmentId",
                table: "MedicalCases",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Patients_PatientId",
                table: "MedicalCases",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Appointments_AppointmentId",
                table: "MedicalCases");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Patients_PatientId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_PatientId",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MedicalCaseTests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "MedicalCases");

            migrationBuilder.AlterColumn<int>(
                name: "AppointmentId",
                table: "MedicalCases",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Appointments_AppointmentId",
                table: "MedicalCases",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
