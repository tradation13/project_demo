using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IPTS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalCaseExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FunctionalAbility",
                table: "MedicalCases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InjuryHistory",
                table: "MedicalCases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Medications",
                table: "MedicalCases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalGoals",
                table: "MedicalCases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FunctionalAbility",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "InjuryHistory",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "Medications",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "PersonalGoals",
                table: "MedicalCases");
        }
    }
}
