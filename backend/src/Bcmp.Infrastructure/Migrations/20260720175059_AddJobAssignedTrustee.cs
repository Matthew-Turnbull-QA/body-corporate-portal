using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bcmp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAssignedTrustee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedTrusteeUserId",
                table: "Jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_AssignedTrusteeUserId",
                table: "Jobs",
                column: "AssignedTrusteeUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_AssignedTrusteeUserId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AssignedTrusteeUserId",
                table: "Jobs");
        }
    }
}
