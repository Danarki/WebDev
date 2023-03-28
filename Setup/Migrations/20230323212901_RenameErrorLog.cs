using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDev.Migrations
{
    /// <inheritdoc />
    public partial class RenameErrorLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ErrorLogs",
                table: "ErrorLogs");

            migrationBuilder.RenameTable(
                name: "ErrorLogs",
                newName: "LogItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LogItems",
                table: "LogItems",
                column: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LogItems",
                table: "LogItems");

            migrationBuilder.RenameTable(
                name: "LogItems",
                newName: "ErrorLogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ErrorLogs",
                table: "ErrorLogs",
                column: "ID");
        }
    }
}
