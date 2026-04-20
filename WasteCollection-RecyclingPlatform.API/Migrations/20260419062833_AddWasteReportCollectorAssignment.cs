using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWasteReportCollectorAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAtUtc",
                table: "waste_reports",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AssignedCollectorId",
                table: "waste_reports",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_waste_reports_AssignedCollectorId",
                table: "waste_reports",
                column: "AssignedCollectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_waste_reports_users_AssignedCollectorId",
                table: "waste_reports",
                column: "AssignedCollectorId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_waste_reports_users_AssignedCollectorId",
                table: "waste_reports");

            migrationBuilder.DropIndex(
                name: "IX_waste_reports_AssignedCollectorId",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "AssignedAtUtc",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "AssignedCollectorId",
                table: "waste_reports");
        }
    }
}
