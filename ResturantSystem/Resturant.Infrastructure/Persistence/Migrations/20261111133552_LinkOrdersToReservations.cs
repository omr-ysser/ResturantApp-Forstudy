using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resturant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkOrdersToReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReservationId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Orders_Reservation_Active",
                table: "Orders",
                column: "ReservationId",
                unique: true,
                filter: "[ReservationId] IS NOT NULL AND [Status] <> N'Completed' AND [Status] <> N'Cancelled'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Reservation_DineIn",
                table: "Orders",
                sql: "[ReservationId] IS NULL\r\nOR\r\n(\r\n    [Type] = N'DineIn'\r\n    AND [DiningTableId] IS NOT NULL\r\n)");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Reservations_ReservationId",
                table: "Orders",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Reservations_ReservationId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "UX_Orders_Reservation_Active",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Reservation_DineIn",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "Orders");
        }
    }
}
