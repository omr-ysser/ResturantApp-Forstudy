using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resturant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModeratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModeratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModerationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.CheckConstraint("CK_Reviews_Deletion", "(\r\n    [IsDeleted] = 0\r\n    AND [DeletedAtUtc] IS NULL\r\n)\r\nOR\r\n(\r\n    [IsDeleted] = 1\r\n    AND [DeletedAtUtc] IS NOT NULL\r\n    AND [Comment] IS NULL\r\n)");
                    table.CheckConstraint("CK_Reviews_DeletionTimestamp", "[DeletedAtUtc] IS NULL\r\nOR [DeletedAtUtc] >= [CreatedAtUtc]");
                    table.CheckConstraint("CK_Reviews_Moderation", "(\r\n    [IsHidden] = 0\r\n    AND [LastModeratedByUserId] IS NULL\r\n    AND [LastModeratedAtUtc] IS NULL\r\n    AND [LastModerationReason] IS NULL\r\n)\r\nOR\r\n(\r\n    [LastModeratedByUserId] IS NOT NULL\r\n    AND [LastModeratedAtUtc] IS NOT NULL\r\n    AND [LastModerationReason] IS NOT NULL\r\n    AND LEN(LTRIM(RTRIM([LastModerationReason]))) > 0\r\n)");
                    table.CheckConstraint("CK_Reviews_ModerationTimestamp", "[LastModeratedAtUtc] IS NULL\r\nOR [LastModeratedAtUtc] >= [CreatedAtUtc]");
                    table.CheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_Reviews_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reviews_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reviews_Users_LastModeratedByUserId",
                        column: x => x.LastModeratedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AuthorUserId_CreatedAtUtc_Id",
                table: "Reviews",
                columns: new[] { "AuthorUserId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_IsDeleted_IsHidden_CreatedAtUtc_Id",
                table: "Reviews",
                columns: new[] { "IsDeleted", "IsHidden", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_LastModeratedByUserId",
                table: "Reviews",
                column: "LastModeratedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Reviews_OrderId",
                table: "Reviews",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");
        }
    }
}
