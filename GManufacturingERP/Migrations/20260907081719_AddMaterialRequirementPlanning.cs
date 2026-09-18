using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GManufacturingERP.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialRequirementPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaterialRequirementPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MRPNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductionPlanId = table.Column<int>(type: "int", nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuyerCompany = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlannedQuantity = table.Column<int>(type: "int", nullable: false),
                    MRPStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRequirementPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRequirementPlans_ProductionPlans_ProductionPlanId",
                        column: x => x.ProductionPlanId,
                        principalTable: "ProductionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialRequirementItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialRequirementPlanId = table.Column<int>(type: "int", nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaterialCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityPerPiece = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WastagePercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRequirementItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRequirementItems_MaterialRequirementPlans_MaterialRequirementPlanId",
                        column: x => x.MaterialRequirementPlanId,
                        principalTable: "MaterialRequirementPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequirementItems_MaterialRequirementPlanId",
                table: "MaterialRequirementItems",
                column: "MaterialRequirementPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequirementPlans_ProductionPlanId",
                table: "MaterialRequirementPlans",
                column: "ProductionPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialRequirementItems");

            migrationBuilder.DropTable(
                name: "MaterialRequirementPlans");
        }
    }
}
