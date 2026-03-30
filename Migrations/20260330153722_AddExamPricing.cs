using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridAcademy.Migrations
{
    /// <inheritdoc />
    public partial class AddExamPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PriceInr",
                table: "exam_pages",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceInr",
                table: "exam_pages");
        }
    }
}
