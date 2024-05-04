using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LazyDan2.Migrations
{
    /// <inheritdoc />
    public partial class Game_Add_State : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Games",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "Games");
        }
    }
}
