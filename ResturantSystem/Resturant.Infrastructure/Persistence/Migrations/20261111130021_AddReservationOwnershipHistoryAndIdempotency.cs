using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resturant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationOwnershipHistoryAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_DiningTableId_StartsAtUtc_EndsAtUtc",
                table: "Reservations");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerUserId",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                table: "Reservations",
                type: "char(64)",
                unicode: false,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestKey",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReservationStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_ReservationStatusHistories", x => x.Id);
                    table.CheckConstraint("CK_ReservationStatusHistories_Reason", "(\r\n    [ToStatus] = N'Cancelled'\r\n    AND [Reason] IS NOT NULL\r\n    AND LEN(LTRIM(RTRIM([Reason]))) > 0\r\n)\r\nOR\r\n(\r\n    [ToStatus] <> N'Cancelled'\r\n    AND [Reason] IS NULL\r\n)");
                    table.CheckConstraint("CK_ReservationStatusHistories_Transition", "(\r\n    [FromStatus] IS NULL\r\n    AND [ToStatus] = N'Confirmed'\r\n)\r\nOR\r\n(\r\n    [FromStatus] IS NOT NULL\r\n    AND\r\n    (\r\n        ([FromStatus] = N'Confirmed'\r\n            AND [ToStatus] = N'Seated')\r\n        OR\r\n        ([FromStatus] = N'Seated'\r\n            AND [ToStatus] = N'Completed')\r\n        OR\r\n        ([FromStatus] = N'Confirmed'\r\n            AND [ToStatus] = N'Cancelled')\r\n        OR\r\n        ([FromStatus] = N'Confirmed'\r\n            AND [ToStatus] = N'NoShow')\r\n    )\r\n)");
                    table.ForeignKey(
                        name: "FK_ReservationStatusHistories_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReservationStatusHistories_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerUserId_StartsAtUtc_Id",
                table: "Reservations",
                columns: new[] { "CustomerUserId", "StartsAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Table_Status_TimeRange",
                table: "Reservations",
                columns: new[] { "DiningTableId", "Status", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Reservations_Creator_RequestKey",
                table: "Reservations",
                columns: new[] { "CreatedByUserId", "RequestKey" },
                unique: true,
                filter: "[CreatedByUserId] IS NOT NULL AND [RequestKey] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservations_RequestMetadata",
                table: "Reservations",
                sql: "(\r\n    [RequestKey] IS NULL\r\n    AND [RequestHash] IS NULL\r\n)\r\nOR\r\n(\r\n    [RequestKey] IS NOT NULL\r\n    AND [RequestHash] IS NOT NULL\r\n    AND [CreatedByUserId] IS NOT NULL\r\n)");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatusHistories_ActorUserId",
                table: "ReservationStatusHistories",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatusHistories_ReservationId_OccurredAtUtc_Id",
                table: "ReservationStatusHistories",
                columns: new[] { "ReservationId", "OccurredAtUtc", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_CreatedByUserId",
                table: "Reservations",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_CustomerUserId",
                table: "Reservations",
                column: "CustomerUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_CreatedByUserId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_CustomerUserId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "ReservationStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CustomerUserId_StartsAtUtc_Id",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_Table_Status_TimeRange",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "UX_Reservations_Creator_RequestKey",
                table: "Reservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservations_RequestMetadata",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RequestKey",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DiningTableId_StartsAtUtc_EndsAtUtc",
                table: "Reservations",
                columns: new[] { "DiningTableId", "StartsAtUtc", "EndsAtUtc" });
        }
    }
}
