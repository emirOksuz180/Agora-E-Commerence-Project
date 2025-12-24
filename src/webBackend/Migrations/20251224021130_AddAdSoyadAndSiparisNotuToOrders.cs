using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdSoyadAndSiparisNotuToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "_EfTrigger",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "_EfTrigger",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
