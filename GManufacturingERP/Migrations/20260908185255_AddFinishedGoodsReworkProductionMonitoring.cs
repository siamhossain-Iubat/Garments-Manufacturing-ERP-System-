using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GManufacturingERP.Migrations
{
    /// <inheritdoc />
    public partial class AddFinishedGoodsReworkProductionMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsReworks_FinishedGoodsStocks_FinishedGoodsStockId",
                table: "FinishedGoodsReworks");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "FinishedGoodsReworks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FinishedGoodsReworks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "FinishedGoodsReworks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "FinishedGoodsReworks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "FinishedGoodsReworks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionMonitoringId",
                table: "FinishedGoodsReworks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsReworks_ProductionMonitoringId",
                table: "FinishedGoodsReworks",
                column: "ProductionMonitoringId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsReworks_FinishedGoodsStocks_FinishedGoodsStockId",
                table: "FinishedGoodsReworks",
                column: "FinishedGoodsStockId",
                principalTable: "FinishedGoodsStocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsReworks_ProductionMonitorings_ProductionMonitoringId",
                table: "FinishedGoodsReworks",
                column: "ProductionMonitoringId",
                principalTable: "ProductionMonitorings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsReworks_FinishedGoodsStocks_FinishedGoodsStockId",
                table: "FinishedGoodsReworks");

            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsReworks_ProductionMonitorings_ProductionMonitoringId",
                table: "FinishedGoodsReworks");

            migrationBuilder.DropIndex(
                name: "IX_FinishedGoodsReworks_ProductionMonitoringId",
                table: "FinishedGoodsReworks");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "FinishedGoodsReworks");

            migrationBuilder.DropColumn(
                name: "ProductionMonitoringId",
                table: "FinishedGoodsReworks");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "FinishedGoodsReworks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FinishedGoodsReworks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "FinishedGoodsReworks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "FinishedGoodsReworks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsReworks_FinishedGoodsStocks_FinishedGoodsStockId",
                table: "FinishedGoodsReworks",
                column: "FinishedGoodsStockId",
                principalTable: "FinishedGoodsStocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
