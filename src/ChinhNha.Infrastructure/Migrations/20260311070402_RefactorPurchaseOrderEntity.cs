using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChinhNha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPurchaseOrderEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceivedDate",
                table: "PurchaseOrders",
                newName: "ExpectedDeliveryDate");

            migrationBuilder.RenameColumn(
                name: "ExpectedDate",
                table: "PurchaseOrders",
                newName: "ActualDeliveryDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderDate",
                table: "PurchaseOrders");

            migrationBuilder.RenameColumn(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders",
                newName: "ReceivedDate");

            migrationBuilder.RenameColumn(
                name: "ActualDeliveryDate",
                table: "PurchaseOrders",
                newName: "ExpectedDate");
        }
    }
}
