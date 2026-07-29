using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bcmp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPermissionsAndLocalPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Permissions",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Permissions" = CASE
                    WHEN "Role" = 'Administrator' THEN 15
                    WHEN "Role" = 'Trustee' THEN 7
                    ELSE 0
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Users");
        }
    }
}
