using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GManufacturingERP.Migrations
{
    /// <inheritdoc />
    public partial class AddFinishedGoodsProductionRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OrderedQuantity",
                table: "FinishedGoodsStocks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProductionPlanId",
                table: "FinishedGoodsStocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionReference",
                table: "FinishedGoodsStocks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReworkQuantity",
                table: "FinishedGoodsStocks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SalesOrderId",
                table: "FinishedGoodsStocks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinishedGoodsReworks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinishedGoodsStockId = table.Column<int>(type: "int", nullable: false),
                    ReworkNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PassedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsReworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsReworks_FinishedGoodsStocks_FinishedGoodsStockId",
                        column: x => x.FinishedGoodsStockId,
                        principalTable: "FinishedGoodsStocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsStocks_ProductionPlanId",
                table: "FinishedGoodsStocks",
                column: "ProductionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsStocks_SalesOrderId",
                table: "FinishedGoodsStocks",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsReworks_FinishedGoodsStockId",
                table: "FinishedGoodsReworks",
                column: "FinishedGoodsStockId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsStocks_ProductionPlans_ProductionPlanId",
                table: "FinishedGoodsStocks",
                column: "ProductionPlanId",
                principalTable: "ProductionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsStocks_SalesOrders_SalesOrderId",
                table: "FinishedGoodsStocks",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsStocks_ProductionPlans_ProductionPlanId",
                table: "FinishedGoodsStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsStocks_SalesOrders_SalesOrderId",
                table: "FinishedGoodsStocks");

            migrationBuilder.DropTable(
                name: "FinishedGoodsReworks");

            migrationBuilder.DropIndex(
                name: "IX_FinishedGoodsStocks_ProductionPlanId",
                table: "FinishedGoodsStocks");

            migrationBuilder.DropIndex(
                name: "IX_FinishedGoodsStocks_SalesOrderId",
                table: "FinishedGoodsStocks");

            migrationBuilder.DropColumn(
                name: "OrderedQuantity",
                table: "FinishedGoodsStocks");

            migrationBuilder.DropColumn(
                name: "ProductionPlanId",
                table: "FinishedGoodsStocks");

            migrationBuilder.DropColumn(
                name: "ProductionReference",
                table: "FinishedGoodsStocks");

            migrationBuilder.DropColumn(
                name: "ReworkQuantity",
                table: "FinishedGoodsStocks");

            migrationBuilder.DropColumn(
                name: "SalesOrderId",
                table: "FinishedGoodsStocks");
        }
    }
}
