using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDev.Migrations
{
    /// <inheritdoc />
    public partial class CardHand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable("DealerHands", null, "CardHands", null);

            migrationBuilder.RenameColumn(
                name: "DealerID",
                table: "CardHands",
                newName: "OwnerID");

            migrationBuilder.AddColumn<bool>(
                name: "OwnerIsDealer",
                table: "CardHands",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerIsDealer",
                table: "CardHands");

            migrationBuilder.RenameColumn(
                name: "OwnerID",
                table: "CardHands",
                newName: "DealerID");
        }
    }
}
