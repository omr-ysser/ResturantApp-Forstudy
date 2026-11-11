using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resturant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteAuthenticationSessionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthenticationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    RefreshCount = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationSessions", x => x.Id);
                    table.UniqueConstraint("AK_AuthenticationSessions_Id_UserId", x => new { x.Id, x.UserId });
                    table.CheckConstraint("CK_AuthenticationSessions_Expiry", "[ExpiresAtUtc] > [StartedAtUtc]");
                    table.CheckConstraint("CK_AuthenticationSessions_RefreshCount", "[RefreshCount] >= 0");
                    table.CheckConstraint("CK_AuthenticationSessions_Revocation", "([RevokedAtUtc] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevocationReason] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_AuthenticationSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId_UserId_ExpiresAtUtc",
                table: "RefreshTokens",
                columns: new[] { "SessionId", "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSessions_UserId_RevokedAtUtc_ExpiresAtUtc",
                table: "AuthenticationSessions",
                columns: new[] { "UserId", "RevokedAtUtc", "ExpiresAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_AuthenticationSessions_SessionId_UserId",
                table: "RefreshTokens",
                columns: new[] { "SessionId", "UserId" },
                principalTable: "AuthenticationSessions",
                principalColumns: new[] { "Id", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_AuthenticationSessions_SessionId_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "AuthenticationSessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_SessionId_UserId_ExpiresAtUtc",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "RefreshTokens");
        }
    }
}
