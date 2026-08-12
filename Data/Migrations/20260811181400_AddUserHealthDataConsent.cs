using System;
using IPTS.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260811181400_AddUserHealthDataConsent")]
    public partial class AddUserHealthDataConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptedHealthDataConsent",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HealthDataConsentAcceptedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedHealthDataConsent",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HealthDataConsentAcceptedAt",
                table: "AspNetUsers");
        }
    }
}
