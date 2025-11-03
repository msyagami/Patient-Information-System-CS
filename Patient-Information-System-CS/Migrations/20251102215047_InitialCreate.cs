using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Patient_Information_System_CS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    AppointmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: true),
                    DoctorId = table.Column<int>(type: "int", nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.AppointmentId);
                });

            migrationBuilder.CreateTable(
                name: "BillingRecords",
                columns: table => new
                {
                    InvoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: true),
                    DoctorName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AdmitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaysStayed = table.Column<int>(type: "int", nullable: false),
                    RoomCharge = table.Column<int>(type: "int", nullable: false),
                    DoctorFee = table.Column<int>(type: "int", nullable: false),
                    MedicineCost = table.Column<int>(type: "int", nullable: false),
                    OtherCharge = table.Column<int>(type: "int", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRecords", x => x.InvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "changeme"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSuperUser = table.Column<bool>(type: "bit", nullable: false),
                    AdminProfile_IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    AdminProfile_ContactNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    StaffProfile_IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    StaffProfile_ContactNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DoctorProfile_Status = table.Column<int>(type: "int", nullable: true),
                    DoctorProfile_Department = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DoctorProfile_ContactNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DoctorProfile_LicenseNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DoctorProfile_Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DoctorProfile_ApplicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientProfile_IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    PatientProfile_HasUnpaidBills = table.Column<bool>(type: "bit", nullable: true),
                    PatientProfile_PatientNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PatientProfile_AdmitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientProfile_RoomAssignment = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PatientProfile_ContactNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PatientProfile_Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PatientProfile_EmergencyContact = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PatientProfile_InsuranceProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PatientProfile_AssignedDoctorId = table.Column<int>(type: "int", nullable: true),
                    PatientProfile_AssignedDoctorName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PatientProfile_DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientProfile_IsCurrentlyAdmitted = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduledFor",
                table: "Appointments",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_IsPaid",
                table: "BillingRecords",
                column: "IsPaid");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_PatientId",
                table: "BillingRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Username",
                table: "UserAccounts",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "BillingRecords");

            migrationBuilder.DropTable(
                name: "UserAccounts");
        }
    }
}
