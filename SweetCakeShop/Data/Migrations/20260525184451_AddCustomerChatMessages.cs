using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SweetCakeShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerChatMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChatToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Sender = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContextProductId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerChatMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerChatMessages_ChatToken",
                table: "CustomerChatMessages",
                column: "ChatToken");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerChatMessages_CreatedAt",
                table: "CustomerChatMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerChatMessages_UserId",
                table: "CustomerChatMessages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerChatMessages");
        }
    }
}
