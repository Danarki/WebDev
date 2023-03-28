using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDev.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreToDealer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameScore",
                table: "Dealers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HandScore",
                table: "Dealers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAce",
                table: "Dealers",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameScore",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "HandScore",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "HasAce",
                table: "Dealers");
        }
    }
}
