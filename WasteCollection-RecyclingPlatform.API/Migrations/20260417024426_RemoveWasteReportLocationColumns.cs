using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWasteReportLocationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_waste_reports_areas_AreaId",
                table: "waste_reports");

            migrationBuilder.DropForeignKey(
                name: "FK_waste_reports_wards_WardId",
                table: "waste_reports");

            migrationBuilder.DropIndex(
                name: "IX_waste_reports_AreaId",
                table: "waste_reports");

            migrationBuilder.DropIndex(
                name: "IX_waste_reports_WardId",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "waste_reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AreaId",
                table: "waste_reports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "waste_reports",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "waste_reports",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "WardId",
                table: "waste_reports",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_waste_reports_AreaId",
                table: "waste_reports",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_waste_reports_WardId",
                table: "waste_reports",
                column: "WardId");

            migrationBuilder.AddForeignKey(
                name: "FK_waste_reports_areas_AreaId",
                table: "waste_reports",
                column: "AreaId",
                principalTable: "areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_waste_reports_wards_WardId",
                table: "waste_reports",
                column: "WardId",
                principalTable: "wards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
