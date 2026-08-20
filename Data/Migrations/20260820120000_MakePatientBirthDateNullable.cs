using IPTS.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260820120000_MakePatientBirthDateNullable")]
    public partial class MakePatientBirthDateNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "BirthDate",
                table: "Patients",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """UPDATE "Patients" SET "BirthDate" = TIMESTAMPTZ '1970-01-01 00:00:00+00' WHERE "BirthDate" IS NULL;""");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BirthDate",
                table: "Patients",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
