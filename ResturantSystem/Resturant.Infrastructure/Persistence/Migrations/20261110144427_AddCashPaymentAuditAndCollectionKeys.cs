using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resturant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashPaymentAuditAndCollectionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderId",
                table: "Payments");

            migrationBuilder.AddColumn<Guid>(
                name: "CollectedByUserId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CollectionRequestKey",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefundedByUserId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CollectedByUserId",
                table: "Payments",
                column: "CollectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RefundedByUserId",
                table: "Payments",
                column: "RefundedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Payments_OnePaidPaymentPerOrder",
                table: "Payments",
                column: "OrderId",
                unique: true,
                filter: "[Status] = N'Paid'");

            migrationBuilder.CreateIndex(
                name: "UX_Payments_OrderId_CollectionRequestKey",
                table: "Payments",
                columns: new[] { "OrderId", "CollectionRequestKey" },
                unique: true,
                filter: "[CollectionRequestKey] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_CashCollectionAudit",
                table: "Payments",
                sql: "(\r\n    [CollectedByUserId] IS NULL\r\n    AND [CollectionRequestKey] IS NULL\r\n)\r\nOR\r\n(\r\n    [CollectedByUserId] IS NOT NULL\r\n    AND [CollectionRequestKey] IS NOT NULL\r\n    AND [Method] = N'Cash'\r\n    AND [PaidAtUtc] IS NOT NULL\r\n    AND [Status] IN (N'Paid', N'Refunded')\r\n)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_RefundAudit",
                table: "Payments",
                sql: "(\r\n    [RefundedByUserId] IS NULL\r\n    AND [RefundReason] IS NULL\r\n)\r\nOR\r\n(\r\n    [RefundedByUserId] IS NOT NULL\r\n    AND [Status] = N'Refunded'\r\n    AND [RefundedAtUtc] IS NOT NULL\r\n    AND [PaidAtUtc] IS NOT NULL\r\n    AND [RefundedAtUtc] >= [PaidAtUtc]\r\n    AND [RefundReason] IS NOT NULL\r\n    AND LEN(LTRIM(RTRIM([RefundReason]))) > 0\r\n)");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_CollectedByUserId",
                table: "Payments",
                column: "CollectedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_RefundedByUserId",
                table: "Payments",
                column: "RefundedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_CollectedByUserId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_RefundedByUserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CollectedByUserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_RefundedByUserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "UX_Payments_OnePaidPaymentPerOrder",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "UX_Payments_OrderId_CollectionRequestKey",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_CashCollectionAudit",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_RefundAudit",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CollectedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CollectionRequestKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundedByUserId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");
        }
    }
}
