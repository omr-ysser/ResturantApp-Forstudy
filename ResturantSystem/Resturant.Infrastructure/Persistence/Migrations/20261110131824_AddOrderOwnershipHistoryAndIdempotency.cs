using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resturant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderOwnershipHistoryAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerUserId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderCreationReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestHash = table.Column<string>(type: "char(64)", unicode: false, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCreationReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderCreationReceipts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderCreationReceipts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusHistories", x => x.Id);
                    table.CheckConstraint("CK_OrderStatusHistories_Reason", "(\r\n    [ToStatus] = N'Cancelled'\r\n    AND [Reason] IS NOT NULL\r\n    AND LEN(LTRIM(RTRIM([Reason]))) > 0\r\n)\r\nOR\r\n(\r\n    [ToStatus] <> N'Cancelled'\r\n    AND [Reason] IS NULL\r\n)");
                    table.CheckConstraint("CK_OrderStatusHistories_Transition", "(\r\n    [FromStatus] IS NULL\r\n    AND [ToStatus] = N'Pending'\r\n)\r\nOR\r\n(\r\n    [FromStatus] IS NOT NULL\r\n    AND\r\n    (\r\n        ([FromStatus] = N'Pending'\r\n            AND [ToStatus] = N'Confirmed')\r\n        OR\r\n        ([FromStatus] = N'Confirmed'\r\n            AND [ToStatus] = N'Preparing')\r\n        OR\r\n        ([FromStatus] = N'Preparing'\r\n            AND [ToStatus] = N'Ready')\r\n        OR\r\n        ([FromStatus] = N'Ready'\r\n            AND [ToStatus] = N'Completed')\r\n        OR\r\n        ([FromStatus] IN (N'Pending', N'Confirmed')\r\n            AND [ToStatus] = N'Cancelled')\r\n    )\r\n)");
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerUserId_CreatedAtUtc_Id",
                table: "Orders",
                columns: new[] { "CustomerUserId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCreationReceipts_OrderId",
                table: "OrderCreationReceipts",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_OrderCreationReceipts_UserId_RequestKey",
                table: "OrderCreationReceipts",
                columns: new[] { "UserId", "RequestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_ActorUserId",
                table: "OrderStatusHistories",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId_OccurredAtUtc_Id",
                table: "OrderStatusHistories",
                columns: new[] { "OrderId", "OccurredAtUtc", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_CustomerUserId",
                table: "Orders",
                column: "CustomerUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_CustomerUserId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderCreationReceipts");

            migrationBuilder.DropTable(
                name: "OrderStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerUserId_CreatedAtUtc_Id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "Orders");
        }
    }
}
