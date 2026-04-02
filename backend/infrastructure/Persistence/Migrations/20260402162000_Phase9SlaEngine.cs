using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase9SlaEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "AssignmentSlaState",
                table: "ServiceRequests",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignmentDueAtUtc",
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
                name: "CompletionDueAtUtc",
                table: "ServiceRequests",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextSlaDeadlineAtUtc",
                table: "ServiceRequests",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ResponseSlaState",
                table: "ServiceRequests",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseDueAtUtc",
                table: "ServiceRequests",
                type: "datetime2(3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentSlaState",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AssignmentDueAtUtc",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "CompletionSlaState",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "CompletionDueAtUtc",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "NextSlaDeadlineAtUtc",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ResponseSlaState",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ResponseDueAtUtc",
                table: "ServiceRequests");
        }
    }
}
