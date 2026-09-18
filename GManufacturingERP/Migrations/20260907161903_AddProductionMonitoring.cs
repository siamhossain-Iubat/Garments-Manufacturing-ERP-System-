using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GManufacturingERP.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionMonitorings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonitoringNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductionReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProducedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionMonitorings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionProgressEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionMonitoringId = table.Column<int>(type: "int", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProducedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalProducedAfterEntry = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingQuantityAfterEntry = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnteredBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionProgressEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionProgressEntries_ProductionMonitorings_ProductionMonitoringId",
                        column: x => x.ProductionMonitoringId,
                        principalTable: "ProductionMonitorings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionProgressEntries_ProductionMonitoringId",
                table: "ProductionProgressEntries",
                column: "ProductionMonitoringId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionProgressEntries");

            migrationBuilder.DropTable(
                name: "ProductionMonitorings");
        }
    }
}
