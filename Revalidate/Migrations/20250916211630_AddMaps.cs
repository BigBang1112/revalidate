using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class AddMaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MapId",
                table: "ValidationResults",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MapUid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sha256 = table.Column<byte[]>(type: "BINARY(32)", nullable: false),
                    GameVersion = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeformattedName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnvironmentId = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeId = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthorTime = table.Column<int>(type: "int", nullable: true),
                    AuthorScore = table.Column<int>(type: "int", nullable: true),
                    NbLaps = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Thumbnail = table.Column<byte[]>(type: "mediumblob", nullable: true),
                    UserUploaded = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maps_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_MapId",
                table: "ValidationResults",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_FileId",
                table: "Maps",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_MapUid",
                table: "Maps",
                column: "MapUid");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_Sha256",
                table: "Maps",
                column: "Sha256",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationResults_Maps_MapId",
                table: "ValidationResults",
                column: "MapId",
                principalTable: "Maps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValidationResults_Maps_MapId",
                table: "ValidationResults");

            migrationBuilder.DropTable(
                name: "Maps");

            migrationBuilder.DropIndex(
                name: "IX_ValidationResults_MapId",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "MapId",
                table: "ValidationResults");
        }
    }
}
