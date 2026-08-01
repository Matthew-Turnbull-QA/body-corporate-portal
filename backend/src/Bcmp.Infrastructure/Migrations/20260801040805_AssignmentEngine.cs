using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bcmp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignmentRuleId",
                table: "Jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentSource",
                table: "Jobs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Jobs"
                SET "AssignmentSource" = 'RoundRobinFallback'
                WHERE "AssignedTrusteeUserId" IS NOT NULL
                  AND "AssignmentSource" IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "AssignmentNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EmailSentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EmailFailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    TargetTrusteeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobSource = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Keywords = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_AssignmentRuleId",
                table: "Jobs",
                column: "AssignmentRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentNotifications_JobId",
                table: "AssignmentNotifications",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentNotifications_RecipientUserId",
                table: "AssignmentNotifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRules_Priority",
                table: "AssignmentRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRules_PropertyId",
                table: "AssignmentRules",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRules_TargetTrusteeUserId",
                table: "AssignmentRules",
                column: "TargetTrusteeUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentNotifications");

            migrationBuilder.DropTable(
                name: "AssignmentRules");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_AssignmentRuleId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AssignmentRuleId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AssignmentSource",
                table: "Jobs");
        }
    }
}
