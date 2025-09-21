using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class AddClientResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "ValidationResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ValidationClientResultEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationClientResultEntity", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_ClientId",
                table: "ValidationResults",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationResults_ValidationClientResultEntity_ClientId",
                table: "ValidationResults",
                column: "ClientId",
                principalTable: "ValidationClientResultEntity",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValidationResults_ValidationClientResultEntity_ClientId",
                table: "ValidationResults");

            migrationBuilder.DropTable(
                name: "ValidationClientResultEntity");

            migrationBuilder.DropIndex(
                name: "IX_ValidationResults_ClientId",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "ValidationResults");
        }
    }
}
