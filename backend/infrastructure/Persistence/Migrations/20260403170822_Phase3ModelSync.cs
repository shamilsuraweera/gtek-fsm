using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3ModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignmentDueAtUtc",
                table: "ServiceRequests",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "AssignmentSlaState",
                table: "ServiceRequests",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletionDueAtUtc",
                table: "ServiceRequests",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "CompletionSlaState",
                table: "ServiceRequests",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextSlaDeadlineAtUtc",
                table: "ServiceRequests",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseDueAtUtc",
                table: "ServiceRequests",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ResponseSlaState",
                table: "ServiceRequests",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "WorkerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkerCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    InternalRating = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    SkillTags = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    BaseLatitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    BaseLongitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    AvailabilityStatus = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerProfiles", x => x.Id);
                    table.UniqueConstraint("AK_WorkerProfiles_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkerProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProfiles_TenantId",
                table: "WorkerProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProfiles_TenantId_DisplayName",
                table: "WorkerProfiles",
                columns: new[] { "TenantId", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "UQ_WorkerProfiles_TenantId_WorkerCode",
                table: "WorkerProfiles",
                columns: new[] { "TenantId", "WorkerCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "AssignmentDueAtUtc",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AssignmentSlaState",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "CompletionDueAtUtc",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "CompletionSlaState",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "NextSlaDeadlineAtUtc",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ResponseDueAtUtc",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ResponseSlaState",
                table: "ServiceRequests");
        }
    }
}
