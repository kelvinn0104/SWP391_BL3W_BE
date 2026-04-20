using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionRecyclingPlatform.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardPointTransactionsAndFinalRewardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinalRewardPoints",
                table: "waste_reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RewardVerifiedAtUtc",
                table: "waste_reports",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reward_point_transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceRefId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_point_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reward_point_transactions_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reward_point_transactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_reward_point_transactions_CreatedAtUtc",
                table: "reward_point_transactions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_reward_point_transactions_CreatedByUserId",
                table: "reward_point_transactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_reward_point_transactions_UserId",
                table: "reward_point_transactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reward_point_transactions");

            migrationBuilder.DropColumn(
                name: "FinalRewardPoints",
                table: "waste_reports");

            migrationBuilder.DropColumn(
                name: "RewardVerifiedAtUtc",
                table: "waste_reports");
        }
    }
}
