using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    /// <inheritdoc />
    public partial class FixDuplicateConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wards_areas_AreaId",
                table: "wards");

            migrationBuilder.DropIndex(
                name: "IX_wards_AreaId",
                table: "wards");

            migrationBuilder.CreateIndex(
                name: "IX_wards_AreaId_Name",
                table: "wards",
                columns: new[] { "AreaId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_areas_DistrictName",
                table: "areas",
                column: "DistrictName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_wards_areas_AreaId",
                table: "wards",
                column: "AreaId",
                principalTable: "areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wards_areas_AreaId",
                table: "wards");

            migrationBuilder.DropIndex(
                name: "IX_wards_AreaId_Name",
                table: "wards");

            migrationBuilder.DropIndex(
                name: "IX_areas_DistrictName",
                table: "areas");

            migrationBuilder.CreateIndex(
                name: "IX_wards_AreaId",
                table: "wards",
                column: "AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_wards_areas_AreaId",
                table: "wards",
                column: "AreaId",
                principalTable: "areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
