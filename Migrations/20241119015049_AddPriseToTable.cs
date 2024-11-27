using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Tranzit_Interface.Migrations
{
    /// <inheritdoc />
    public partial class AddPriseToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Prise",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prise",
                table: "Products");
        }
    }
}
