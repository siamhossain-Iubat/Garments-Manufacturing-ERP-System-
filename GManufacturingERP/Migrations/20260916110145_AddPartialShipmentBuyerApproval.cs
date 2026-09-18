using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GManufacturingERP.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialShipmentBuyerApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartialShipmentRequestId",
                table: "Packings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PartialShipmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    FinishedGoodsStockId = table.Column<int>(type: "int", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingOrderQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BuyerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialShipmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartialShipmentRequests_FinishedGoodsStocks_FinishedGoodsStockId",
                        column: x => x.FinishedGoodsStockId,
                        principalTable: "FinishedGoodsStocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartialShipmentRequests_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PartialShipmentRequestId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_PartialShipmentRequests_PartialShipmentRequestId",
                        column: x => x.PartialShipmentRequestId,
                        principalTable: "PartialShipmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Packings_PartialShipmentRequestId",
                table: "Packings",
                column: "PartialShipmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PartialShipmentRequestId",
                table: "Notifications",
                column: "PartialShipmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PartialShipmentRequests_FinishedGoodsStockId",
                table: "PartialShipmentRequests",
                column: "FinishedGoodsStockId");

            migrationBuilder.CreateIndex(
                name: "IX_PartialShipmentRequests_SalesOrderId",
                table: "PartialShipmentRequests",
                column: "SalesOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Packings_PartialShipmentRequests_PartialShipmentRequestId",
                table: "Packings",
                column: "PartialShipmentRequestId",
                principalTable: "PartialShipmentRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Packings_PartialShipmentRequests_PartialShipmentRequestId",
                table: "Packings");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PartialShipmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_Packings_PartialShipmentRequestId",
                table: "Packings");

            migrationBuilder.DropColumn(
                name: "PartialShipmentRequestId",
                table: "Packings");
        }
    }
}
