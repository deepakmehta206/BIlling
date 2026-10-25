using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GuestPlayDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayDiscountPercent",
                table: "GuestOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayScore",
                table: "GuestOrders",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayDiscountPercent",
                table: "GuestOrders");

            migrationBuilder.DropColumn(
                name: "PlayScore",
                table: "GuestOrders");
        }
    }
}
