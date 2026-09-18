using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GManufacturingERP.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialRequirementPlanToMaterialIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductionPlanId",
                table: "ProductionMonitorings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaterialRequirementPlanId",
                table: "MaterialIssues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionPlanId",
                table: "MaterialIssues",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMonitorings_ProductionPlanId",
                table: "ProductionMonitorings",
                column: "ProductionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_MaterialRequirementPlanId",
                table: "MaterialIssues",
                column: "MaterialRequirementPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_ProductionPlanId",
                table: "MaterialIssues",
                column: "ProductionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialIssues_MaterialRequirementPlans_MaterialRequirementPlanId",
                table: "MaterialIssues",
                column: "MaterialRequirementPlanId",
                principalTable: "MaterialRequirementPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialIssues_ProductionPlans_ProductionPlanId",
                table: "MaterialIssues",
                column: "ProductionPlanId",
                principalTable: "ProductionPlans",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionMonitorings_ProductionPlans_ProductionPlanId",
                table: "ProductionMonitorings",
                column: "ProductionPlanId",
                principalTable: "ProductionPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialIssues_MaterialRequirementPlans_MaterialRequirementPlanId",
                table: "MaterialIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialIssues_ProductionPlans_ProductionPlanId",
                table: "MaterialIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionMonitorings_ProductionPlans_ProductionPlanId",
                table: "ProductionMonitorings");

            migrationBuilder.DropIndex(
                name: "IX_ProductionMonitorings_ProductionPlanId",
                table: "ProductionMonitorings");

            migrationBuilder.DropIndex(
                name: "IX_MaterialIssues_MaterialRequirementPlanId",
                table: "MaterialIssues");

            migrationBuilder.DropIndex(
                name: "IX_MaterialIssues_ProductionPlanId",
                table: "MaterialIssues");

            migrationBuilder.DropColumn(
                name: "ProductionPlanId",
                table: "ProductionMonitorings");

            migrationBuilder.DropColumn(
                name: "MaterialRequirementPlanId",
                table: "MaterialIssues");

            migrationBuilder.DropColumn(
                name: "ProductionPlanId",
                table: "MaterialIssues");
        }
    }
}
