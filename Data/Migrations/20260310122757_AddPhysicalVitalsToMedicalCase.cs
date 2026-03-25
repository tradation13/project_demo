using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalVitalsToMedicalCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "ActivityLevel",
                table: "MedicalCases",
                type: "smallint",
                nullable: true,
                comment: "0: Sedentary, 1: Moderate, 2: Active, 3: Professional");

            migrationBuilder.AddColumn<byte>(
                name: "BloodGroup",
                table: "MedicalCases",
                type: "smallint",
                nullable: true,
                comment: "0: A+, 1: A-, 2: B+, 3: B-, 4: O+, 5: O-, 6: AB+, 7: AB-");

            migrationBuilder.AddColumn<byte>(
                name: "DominantSide",
                table: "MedicalCases",
                type: "smallint",
                nullable: true,
                comment: "0: RightSide, 1: LeftSide");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "DominantSide",
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
    }
}
