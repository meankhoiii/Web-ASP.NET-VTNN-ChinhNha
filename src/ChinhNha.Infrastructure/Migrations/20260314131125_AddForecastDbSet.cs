using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChinhNha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddForecastDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryForecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ForecastDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PredictedDemand = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ConfidenceLower = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ConfidenceUpper = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActualDemand = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MAPE = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryForecasts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryForecasts_ProductId",
                table: "InventoryForecasts",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryForecasts");
        }
    }
}
