using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWasteReportItemImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WasteReportItemId",
                table: "waste_report_images",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_waste_report_images_WasteReportItemId",
                table: "waste_report_images",
                column: "WasteReportItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_waste_report_images_waste_report_items_WasteReportItemId",
                table: "waste_report_images",
                column: "WasteReportItemId",
                principalTable: "waste_report_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_waste_report_images_waste_report_items_WasteReportItemId",
                table: "waste_report_images");

            migrationBuilder.DropIndex(
                name: "IX_waste_report_images_WasteReportItemId",
                table: "waste_report_images");

            migrationBuilder.DropColumn(
                name: "WasteReportItemId",
                table: "waste_report_images");
        }
    }
}
