using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDev.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGameScoreTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gamescore");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "GameRoom",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "GameScore",
                table: "ConnectedUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HandScore",
                table: "ConnectedUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAce",
                table: "ConnectedUsers",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameScore",
                table: "ConnectedUsers");

            migrationBuilder.DropColumn(
                name: "HandScore",
                table: "ConnectedUsers");

            migrationBuilder.DropColumn(
                name: "HasAce",
                table: "ConnectedUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "GameRoom",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateTable(
                name: "gamescore",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameID = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gamescore", x => x.ID);
                });
        }
    }
}
