using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValidationDistroResults_ValidationResults_ResultId",
                table: "ValidationDistroResults");

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationDistroResults_ValidationResults_ResultId",
                table: "ValidationDistroResults",
                column: "ResultId",
                principalTable: "ValidationResults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValidationDistroResults_ValidationResults_ResultId",
                table: "ValidationDistroResults");

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationDistroResults_ValidationResults_ResultId",
                table: "ValidationDistroResults",
                column: "ResultId",
                principalTable: "ValidationResults",
                principalColumn: "Id");
        }
    }
}
