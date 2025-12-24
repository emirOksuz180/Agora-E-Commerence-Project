using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrdersTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropTable(
            //     name: "OrderDetails");

            // migrationBuilder.DropTable(
            //     name: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.CreateTable(
            //     name: "OrderDetails",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         ProductId = table.Column<int>(type: "int", nullable: false),
            //         OrderId = table.Column<int>(type: "int", nullable: false),
            //         Quantity = table.Column<int>(type: "int", nullable: false),
            //         UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK__OrderDet__3214EC075EA405D1", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK__OrderDeta__Products__5165187F",
            //             column: x => x.ProductId,
            //             principalTable: "Products",
            //             principalColumn: "ProductId",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            // migrationBuilder.CreateTable(
            //     name: "Orders",
            //     columns: table => new
            //     {
            //         OrderId = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         UserId = table.Column<int>(type: "int", nullable: false),
            //         CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
            //         Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
            //         TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK__Orders__C3905BCF18D13ABA", x => x.OrderId);
            //         table.ForeignKey(
            //             name: "FK__Orders__UserId__4D94879B",
            //             column: x => x.UserId,
            //             principalTable: "Users",
            //             principalColumn: "UserId",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            // migrationBuilder.CreateIndex(
            //     name: "IX_OrderDetails_ProductId",
            //     table: "OrderDetails",
            //     column: "ProductId");

            // migrationBuilder.CreateIndex(
            //     name: "IX_Orders_UserId",
            //     table: "Orders",
            //     column: "UserId");
        }
    }
}
