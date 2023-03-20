using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDev.Migrations
{
    /// <inheritdoc />
    public partial class AddInUseField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InUse",
                table: "DeckCards",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InUse",
                table: "DeckCards");
        }
    }
}
