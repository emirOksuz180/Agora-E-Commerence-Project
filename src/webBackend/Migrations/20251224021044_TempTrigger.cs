using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webBackend.Migrations
{
    /// <inheritdoc />
    public partial class TempTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "_EfTrigger",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "_EfTrigger",
                table: "Orders");
        }
    }
}
