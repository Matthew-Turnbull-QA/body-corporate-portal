using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bcmp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TrusteeOnlyPortalAdmins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPortalAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "IsPortalAdmin" = TRUE
                WHERE "Role" = 'Administrator'
                """);

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Permissions",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Trustee");

            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Role" = CASE
                        WHEN "IsPortalAdmin" THEN 'Administrator'
                        ELSE 'Trustee'
                    END,
                    "Permissions" = CASE
                        WHEN "IsPortalAdmin" THEN 15
                        ELSE 7
                    END
                """);

            migrationBuilder.DropColumn(
                name: "IsPortalAdmin",
                table: "Users");
        }
    }
}
