using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDev.Migrations
{
    /// <inheritdoc />
    public partial class AddGameAndGameScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameRoom_GameTypes_GameID",
                table: "GameRoom");

            migrationBuilder.DropTable(
                name: "Connection");

            migrationBuilder.DropTable(
                name: "GameRoomUser");

            migrationBuilder.DropIndex(
                name: "IX_GameRoom_GameID",
                table: "GameRoom");

            migrationBuilder.AddColumn<int>(
                name: "MaxScore",
                table: "GameTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GameID",
                table: "ConnectedUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "game",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameID = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "gamescore",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    GameID = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gamescore", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game");

            migrationBuilder.DropTable(
                name: "gamescore");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "GameTypes");

            migrationBuilder.DropColumn(
                name: "GameID",
                table: "ConnectedUsers");

            migrationBuilder.CreateTable(
                name: "Connection",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsConnected = table.Column<bool>(type: "bit", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connection", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Connection_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "GameRoomUser",
                columns: table => new
                {
                    RoomsID = table.Column<int>(type: "int", nullable: false),
                    UsersID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRoomUser", x => new { x.RoomsID, x.UsersID });
                    table.ForeignKey(
                        name: "FK_GameRoomUser_GameRoom_RoomsID",
                        column: x => x.RoomsID,
                        principalTable: "GameRoom",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameRoomUser_User_UsersID",
                        column: x => x.UsersID,
                        principalTable: "User",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameRoom_GameID",
                table: "GameRoom",
                column: "GameID");

            migrationBuilder.CreateIndex(
                name: "IX_Connection_UserID",
                table: "Connection",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_GameRoomUser_UsersID",
                table: "GameRoomUser",
                column: "UsersID");

            migrationBuilder.AddForeignKey(
                name: "FK_GameRoom_GameTypes_GameID",
                table: "GameRoom",
                column: "GameID",
                principalTable: "GameTypes",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
