using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    public partial class FinalSchemaUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Đảm bảo thêm vào đúng bảng collection_requests (viết thường)
            migrationBuilder.AddColumn<long>(
                name: "WardId",
                table: "collection_requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CitizenName",
                table: "collection_requests",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CollectorName",
                table: "collection_requests",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CollectorPhone",
                table: "collection_requests",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_collection_requests_WardId",
                table: "collection_requests",
                column: "WardId");

            migrationBuilder.AddForeignKey(
                name: "FK_collection_requests_wards_WardId",
                table: "collection_requests",
                column: "WardId",
                principalTable: "wards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_collection_requests_wards_WardId",
                table: "collection_requests");

            migrationBuilder.DropIndex(
                name: "IX_collection_requests_WardId",
                table: "collection_requests");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "collection_requests");

            migrationBuilder.DropColumn(
                name: "CitizenName",
                table: "collection_requests");

            migrationBuilder.DropColumn(
                name: "CollectorName",
                table: "collection_requests");
        }
    }
}
