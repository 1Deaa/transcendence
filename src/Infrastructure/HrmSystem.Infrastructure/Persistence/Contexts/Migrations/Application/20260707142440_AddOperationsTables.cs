using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrmSystem.Infrastructure.Persistence.Contexts.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddOperationsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupHistory",
                schema: "HrmSystem",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetainedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(360)", maxLength: 360, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(360)", maxLength: 360, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealthCheckSnapshots",
                schema: "HrmSystem",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationMs = table.Column<double>(type: "float", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCheckSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistory_Status_StartedAtUtc",
                schema: "HrmSystem",
                table: "BackupHistory",
                columns: new[] { "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckSnapshots_CheckedAtUtc",
                schema: "HrmSystem",
                table: "HealthCheckSnapshots",
                column: "CheckedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckSnapshots_ComponentName_CheckedAtUtc",
                schema: "HrmSystem",
                table: "HealthCheckSnapshots",
                columns: new[] { "ComponentName", "CheckedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupHistory",
                schema: "HrmSystem");

            migrationBuilder.DropTable(
                name: "HealthCheckSnapshots",
                schema: "HrmSystem");
        }
    }
}
