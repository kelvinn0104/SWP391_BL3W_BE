using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WasteCollection_RecyclingPlatform.Repositories.Data;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260419090000_AddCollectorJobCompletionFlow")]
    public partial class AddCollectorJobCompletionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualTotalWeightKg",
                table: "waste_reports",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "waste_reports",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNote",
                table: "waste_reports",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualWeightKg",
                table: "waste_report_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "waste_report_images",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ReportEvidence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualTotalWeightKg",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "CompletionNote",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "ActualWeightKg",
                table: "waste_report_items");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "waste_report_images");
        }
    }
}
