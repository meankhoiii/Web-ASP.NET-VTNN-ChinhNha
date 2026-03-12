using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChinhNha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedSearchFilters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FilterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FiltersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearchFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedSearchFilters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchAnalytics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Query = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchCount = table.Column<int>(type: "int", nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FiltersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstSearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AverageViewTimeSeconds = table.Column<int>(type: "int", nullable: true),
                    ConvertedToView = table.Column<bool>(type: "bit", nullable: false),
                    ConvertedProductId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchAnalytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchAnalytics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearchFilters_UserId",
                table: "SavedSearchFilters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchAnalytics_UserId",
                table: "SearchAnalytics",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedSearchFilters");

            migrationBuilder.DropTable(
                name: "SearchAnalytics");
        }
    }
}
