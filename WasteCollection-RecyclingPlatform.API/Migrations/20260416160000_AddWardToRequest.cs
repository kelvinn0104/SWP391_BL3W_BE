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
                table: "CollectionRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRequests_WardId",
                table: "CollectionRequests",
                column: "WardId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionRequests_Wards_WardId",
                table: "CollectionRequests",
                column: "WardId",
                principalTable: "Wards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionRequests_Wards_WardId",
                table: "CollectionRequests");

            migrationBuilder.DropIndex(
                name: "IX_CollectionRequests_WardId",
                table: "CollectionRequests");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "CollectionRequests");
        }
    }
}
