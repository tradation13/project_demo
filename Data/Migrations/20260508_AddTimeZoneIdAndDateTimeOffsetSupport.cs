using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeZoneIdAndDateTimeOffsetSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // إضافة TimeZoneId إلى جدول Doctors
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Doctors",
                type: "text",
                nullable: false,
                defaultValue: "W. Europe Standard Time");

            // إعادة تشكيل جدول Appointments لتغيير ScheduledTime من DateTime إلى DateTimeOffset
            // أولاً: نسقط القيد الأجنبي والفهرس
            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments");

            // حذف العمود القديم والعمود الجديد
            migrationBuilder.DropColumn(
                name: "ScheduledTime",
                table: "Appointments");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledTime",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // إعادة إنشاء الفهرسات والقيود الأجنبية
            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // العودة للخطوة السابقة
            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ScheduledTime",
                table: "Appointments");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledTime",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Doctors");
        }
    }
}
