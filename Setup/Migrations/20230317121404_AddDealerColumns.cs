using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDev.Migrations
{
    /// <inheritdoc />
    public partial class AddDealerColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardHands",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DealerID = table.Column<int>(type: "int", nullable: false),
                    CardID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerHands", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Dealers",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealers", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DeckCards",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameID = table.Column<int>(type: "int", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rank = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckCards", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardHands");

            migrationBuilder.DropTable(
                name: "Dealers");

            migrationBuilder.DropTable(
                name: "DeckCards");
        }
    }
}
