using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWardToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WardId",
                table: "collection_requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRequests_WardId",
                table: "collection_requests",
                column: "WardId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionRequests_Wards_WardId",
                table: "collection_requests",
                column: "WardId",
                principalTable: "wards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionRequests_Wards_WardId",
                table: "collection_requests");

            migrationBuilder.DropIndex(
                name: "IX_CollectionRequests_WardId",
                table: "collection_requests");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "collection_requests");
        }
    }
}
