using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SweetCakeShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderConfirmedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Orders");
        }
    }
}
