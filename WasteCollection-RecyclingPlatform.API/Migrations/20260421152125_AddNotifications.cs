using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_collection_requests_users_CitizenId",
            //     table: "collection_requests");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_collection_requests_users_CollectorId",
            //     table: "collection_requests");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_collection_requests_wards_WardId",
            //     table: "collection_requests");

            // migrationBuilder.DropPrimaryKey(
            //     name: "PK_collection_requests",
            //     table: "collection_requests");

            // migrationBuilder.RenameTable(
            //     name: "collection_requests",
            //     newName: "CollectionRequests");

            // migrationBuilder.RenameIndex(
            //     name: "IX_collection_requests_WardId",
            //     table: "CollectionRequests",
            //     newName: "IX_CollectionRequests_WardId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_collection_requests_CollectorId",
            //     table: "CollectionRequests",
            //     newName: "IX_CollectionRequests_CollectorId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_collection_requests_CitizenId",
            //     table: "CollectionRequests",
            //     newName: "IX_CollectionRequests_CitizenId");

            // migrationBuilder.AddColumn<long>(
            //     name: "WardId",
            //     table: "waste_reports",
            //     type: "bigint",
            //     nullable: true);

            // migrationBuilder.AlterColumn<decimal>(
            //     name: "WeightKg",
            //     table: "CollectionRequests",
            //     type: "decimal(65,30)",
            //     nullable: false,
            //     oldClrType: typeof(decimal),
            //     oldType: "decimal(18,2)",
            //     oldPrecision: 18,
            //     oldScale: 2);

            // migrationBuilder.AlterColumn<int>(
            //     name: "Status",
            //     table: "CollectionRequests",
            //     type: "int",
            //     nullable: false,
            //     oldClrType: typeof(string),
            //     oldType: "varchar(32)",
            //     oldMaxLength: 32)
            //     .OldAnnotation("MySql:CharSet", "utf8mb4");

            // migrationBuilder.AddPrimaryKey(
            //     name: "PK_CollectionRequests",
            //     table: "CollectionRequests",
            //     column: "Id");

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RecipientUserId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Body = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedReportId = table.Column<long>(type: "bigint", nullable: true),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // migrationBuilder.CreateIndex(
            //     name: "IX_waste_reports_WardId",
            //     table: "waste_reports",
            //     column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_IsRead",
                table: "notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_RecipientUserId",
                table: "notifications",
                column: "RecipientUserId");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_CollectionRequests_users_CitizenId",
            //     table: "CollectionRequests",
            //     column: "CitizenId",
            //     principalTable: "users",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Cascade);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_CollectionRequests_users_CollectorId",
            //     table: "CollectionRequests",
            //     column: "CollectorId",
            //     principalTable: "users",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_CollectionRequests_wards_WardId",
            //     table: "CollectionRequests",
            //     column: "WardId",
            //     principalTable: "wards",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_waste_reports_wards_WardId",
            //     table: "waste_reports",
            //     column: "WardId",
            //     principalTable: "wards",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionRequests_users_CitizenId",
                table: "CollectionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_CollectionRequests_users_CollectorId",
                table: "CollectionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_CollectionRequests_wards_WardId",
                table: "CollectionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_waste_reports_wards_WardId",
                table: "waste_reports");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_waste_reports_WardId",
                table: "waste_reports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CollectionRequests",
                table: "CollectionRequests");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "waste_reports");

            migrationBuilder.RenameTable(
                name: "CollectionRequests",
                newName: "collection_requests");

            migrationBuilder.RenameIndex(
                name: "IX_CollectionRequests_WardId",
                table: "collection_requests",
                newName: "IX_collection_requests_WardId");

            migrationBuilder.RenameIndex(
                name: "IX_CollectionRequests_CollectorId",
                table: "collection_requests",
                newName: "IX_collection_requests_CollectorId");

            migrationBuilder.RenameIndex(
                name: "IX_CollectionRequests_CitizenId",
                table: "collection_requests",
                newName: "IX_collection_requests_CitizenId");

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightKg",
                table: "collection_requests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "collection_requests",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_collection_requests",
                table: "collection_requests",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_collection_requests_users_CitizenId",
                table: "collection_requests",
                column: "CitizenId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_collection_requests_users_CollectorId",
                table: "collection_requests",
                column: "CollectorId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_collection_requests_wards_WardId",
                table: "collection_requests",
                column: "WardId",
                principalTable: "wards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
