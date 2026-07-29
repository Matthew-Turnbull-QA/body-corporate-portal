using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bcmp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkAccessRequestsToExistingUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExistingUserId",
                table: "AccessRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_ExistingUserId",
                table: "AccessRequests",
                column: "ExistingUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccessRequests_ExistingUserId",
                table: "AccessRequests");

            migrationBuilder.DropColumn(
                name: "ExistingUserId",
                table: "AccessRequests");
        }
    }
}
