using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GManufacturingERP.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityInspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InspectionNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductionMonitoringId = table.Column<int>(type: "int", nullable: false),
                    ProductionReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProducedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InspectedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PassedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FailedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InspectionResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InspectedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DefectDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityInspections_ProductionMonitorings_ProductionMonitoringId",
                        column: x => x.ProductionMonitoringId,
                        principalTable: "ProductionMonitorings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_ProductionMonitoringId",
                table: "QualityInspections",
                column: "ProductionMonitoringId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QualityInspections");
        }
    }
}
