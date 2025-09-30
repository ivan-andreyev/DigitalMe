using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalMe.Migrations
{
    /// <inheritdoc />
    public partial class FixTestingEnvironmentDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticationTag",
                table: "ApiConfigurations",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthenticationTag",
                table: "ApiConfigurations");
        }
    }
}
