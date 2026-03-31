using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3SubscriptionUserLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserLimit",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 25);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserLimit",
                table: "Subscriptions");
        }
    }
}
