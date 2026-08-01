using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bcmp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EmailIntakeJobNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<long>(
                name: "JobNumberSequence");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "PropertyId",
                table: "Jobs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "JobNumber",
                table: "Jobs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH numbered AS (
                    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "CreatedAtUtc", "Id") AS row_number
                    FROM "Jobs"
                )
                UPDATE "Jobs"
                SET "JobNumber" = 'BCMP-' || LPAD(numbered.row_number::text, 6, '0')
                FROM numbered
                WHERE "Jobs"."Id" = numbered."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "JobNumber",
                table: "Jobs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.Sql(
                """
                SELECT setval(
                    '"JobNumberSequence"',
                    COALESCE((SELECT MAX(CAST(SUBSTRING("JobNumber" FROM 6) AS bigint)) FROM "Jobs"), 0) + 1,
                    false);
                """);

            migrationBuilder.CreateTable(
                name: "EmailIntakeMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderMessageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MessageId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SenderEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    SenderDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailIntakeMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobNumber",
                table: "Jobs",
                column: "JobNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailIntakeMessages_JobId",
                table: "EmailIntakeMessages",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailIntakeMessages_MessageId",
                table: "EmailIntakeMessages",
                column: "MessageId",
                unique: true,
                filter: "\"MessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmailIntakeMessages_ProviderMessageKey",
                table: "EmailIntakeMessages",
                column: "ProviderMessageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailIntakeMessages");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_JobNumber",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JobNumber",
                table: "Jobs");

            migrationBuilder.DropSequence(
                name: "JobNumberSequence");

            migrationBuilder.AlterColumn<Guid>(
                name: "PropertyId",
                table: "Jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
