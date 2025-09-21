using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class RenameToValidationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValidationDistroResults_ValidationLogEntity_LogId",
                table: "ValidationDistroResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ValidationLogEntity",
                table: "ValidationLogEntity");

            migrationBuilder.RenameTable(
                name: "ValidationLogEntity",
                newName: "ValidationLogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ValidationLogs",
                table: "ValidationLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationDistroResults_ValidationLogs_LogId",
                table: "ValidationDistroResults",
                column: "LogId",
                principalTable: "ValidationLogs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValidationDistroResults_ValidationLogs_LogId",
                table: "ValidationDistroResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ValidationLogs",
                table: "ValidationLogs");

            migrationBuilder.RenameTable(
                name: "ValidationLogs",
                newName: "ValidationLogEntity");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ValidationLogEntity",
                table: "ValidationLogEntity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationDistroResults_ValidationLogEntity_LogId",
                table: "ValidationDistroResults",
                column: "LogId",
                principalTable: "ValidationLogEntity",
                principalColumn: "Id");
        }
    }
}
