using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Data.Migrations
{
    /// <inheritdoc />
    public partial class MovePatientHealthFieldsToPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "BloodGroup",
                table: "Patients",
                type: "smallint",
                nullable: true,
                comment: "0: A+, 1: A-, 2: B+, 3: B-, 4: O+, 5: O-, 6: AB+, 7: AB-");

            migrationBuilder.AddColumn<bool>(
                name: "HasChronicDisease",
                table: "Patients",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "Patients",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSmoker",
                table: "Patients",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Weight",
                table: "Patients",
                type: "real",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Patients"" AS p
                SET
                    ""BloodGroup""        = latest.""BloodGroup"",
                    ""Height""            = latest.""Height"",
                    ""Weight""            = latest.""Weight"",
                    ""IsSmoker""          = latest.""IsSmoker"",
                    ""HasChronicDisease"" = latest.""HasChronicDisease""
                FROM (
                    SELECT DISTINCT ON (""PatientId"")
                        ""PatientId"", ""BloodGroup"", ""Height"", ""Weight"", ""IsSmoker"", ""HasChronicDisease""
                    FROM ""MedicalCases""
                    ORDER BY ""PatientId"", ""CreatedAt"" DESC
                ) AS latest
                WHERE p.""Id"" = latest.""PatientId"";
            ");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "HasChronicDisease",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "IsSmoker",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "MedicalCases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "BloodGroup",
                table: "MedicalCases",
                type: "smallint",
                nullable: true,
                comment: "0: A+, 1: A-, 2: B+, 3: B-, 4: O+, 5: O-, 6: AB+, 7: AB-");

            migrationBuilder.AddColumn<bool>(
                name: "HasChronicDisease",
                table: "MedicalCases",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "MedicalCases",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSmoker",
                table: "MedicalCases",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Weight",
                table: "MedicalCases",
                type: "real",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""MedicalCases"" AS mc
                SET
                    ""BloodGroup""        = p.""BloodGroup"",
                    ""Height""            = p.""Height"",
                    ""Weight""            = p.""Weight"",
                    ""IsSmoker""          = p.""IsSmoker"",
                    ""HasChronicDisease"" = p.""HasChronicDisease""
                FROM ""Patients"" AS p
                WHERE mc.""PatientId"" = p.""Id"";
            ");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "HasChronicDisease",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IsSmoker",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Patients");
        }
    }
}
