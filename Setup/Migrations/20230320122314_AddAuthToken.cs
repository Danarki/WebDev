using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDev.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthToken",
                table: "ConnectedUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthToken",
                table: "ConnectedUsers");
        }
    }
}
