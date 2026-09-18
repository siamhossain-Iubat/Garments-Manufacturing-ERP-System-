using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GManufacturingERP.Migrations
{
    /// <inheritdoc />
    public partial class AddPackingShipmentApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Packings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "Packings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PartialShipmentApproved",
                table: "Packings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentType",
                table: "Packings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Packings");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Packings");

            migrationBuilder.DropColumn(
                name: "PartialShipmentApproved",
                table: "Packings");

            migrationBuilder.DropColumn(
                name: "ShipmentType",
                table: "Packings");
        }
    }
}
