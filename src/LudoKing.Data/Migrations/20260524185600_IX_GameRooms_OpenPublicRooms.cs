using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LudoKing.Data.Migrations
{
    /// <inheritdoc />
    public partial class IX_GameRooms_OpenPublicRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GameRooms_Status_IsPrivate_CreatedAt",
                table: "GameRooms",
                columns: new[] { "Status", "IsPrivate", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameRooms_Status_IsPrivate_CreatedAt",
                table: "GameRooms");
        }
    }
}
