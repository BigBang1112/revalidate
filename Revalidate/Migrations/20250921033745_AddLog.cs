using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class AddLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LogId",
                table: "ValidationDistroResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ValidationLogEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Log = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationLogEntity", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationDistroResults_LogId",
                table: "ValidationDistroResults",
                column: "LogId");

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationDistroResults_ValidationLogEntity_LogId",
                table: "ValidationDistroResults",
                column: "LogId",
                principalTable: "ValidationLogEntity",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValidationDistroResults_ValidationLogEntity_LogId",
                table: "ValidationDistroResults");

            migrationBuilder.DropTable(
                name: "ValidationLogEntity");

            migrationBuilder.DropIndex(
                name: "IX_ValidationDistroResults_LogId",
                table: "ValidationDistroResults");

            migrationBuilder.DropColumn(
                name: "LogId",
                table: "ValidationDistroResults");
        }
    }
}
