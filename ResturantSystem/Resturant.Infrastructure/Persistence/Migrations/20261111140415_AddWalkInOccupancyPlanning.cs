using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resturant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalkInOccupancyPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_DiningTableId",
                table: "Orders");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DiningStartedAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpectedDepartureAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DiningTableId_Status_ExpectedDepartureAtUtc",
                table: "Orders",
                columns: new[] { "DiningTableId", "Status", "ExpectedDepartureAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_WalkInPlan",
                table: "Orders",
                sql: "(\r\n    [DiningStartedAtUtc] IS NULL\r\n    AND [ExpectedDepartureAtUtc] IS NULL\r\n)\r\nOR\r\n(\r\n    [DiningStartedAtUtc] IS NOT NULL\r\n    AND [ExpectedDepartureAtUtc] IS NOT NULL\r\n    AND [ExpectedDepartureAtUtc] > [DiningStartedAtUtc]\r\n    AND [Type] = N'DineIn'\r\n    AND [DiningTableId] IS NOT NULL\r\n    AND [ReservationId] IS NULL\r\n)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_DiningTableId_Status_ExpectedDepartureAtUtc",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_WalkInPlan",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiningStartedAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExpectedDepartureAtUtc",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DiningTableId",
                table: "Orders",
                column: "DiningTableId");
        }
    }
}
