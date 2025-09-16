using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class AddDistroResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "DeclaredNbCheckpoints",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "DeclaredNbRespawns",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "DeclaredScore",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "DeclaredTime",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "InputsResult",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "RawJsonResult",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "ValidatedNbCheckpoints",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "ValidatedNbRespawns",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "ValidatedScore",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "ValidatedTime",
                table: "ValidationResults");

            migrationBuilder.AddColumn<bool>(
                name: "IsValidExtracted",
                table: "ValidationResults",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ValidationDistroResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ResultId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DistroId = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsValid = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsValidExtracted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeclaredNbCheckpoints = table.Column<int>(type: "int", nullable: false),
                    DeclaredNbRespawns = table.Column<int>(type: "int", nullable: false),
                    DeclaredTime = table.Column<int>(type: "int", nullable: true),
                    DeclaredScore = table.Column<int>(type: "int", nullable: false),
                    ValidatedNbCheckpoints = table.Column<int>(type: "int", nullable: true),
                    ValidatedNbRespawns = table.Column<int>(type: "int", nullable: true),
                    ValidatedTime = table.Column<int>(type: "int", nullable: true),
                    ValidatedScore = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    InputsResult = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawJsonResult = table.Column<string>(type: "longtext", maxLength: 32767, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationDistroResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationDistroResults_ValidationResults_ResultId",
                        column: x => x.ResultId,
                        principalTable: "ValidationResults",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationDistroResults_ResultId",
                table: "ValidationDistroResults",
                column: "ResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidationDistroResults");

            migrationBuilder.DropColumn(
                name: "IsValidExtracted",
                table: "ValidationResults");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "ValidationResults",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "DeclaredNbCheckpoints",
                table: "ValidationResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeclaredNbRespawns",
                table: "ValidationResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeclaredScore",
                table: "ValidationResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeclaredTime",
                table: "ValidationResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputsResult",
                table: "ValidationResults",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RawJsonResult",
                table: "ValidationResults",
                type: "longtext",
                maxLength: 32767,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ValidatedNbCheckpoints",
                table: "ValidationResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValidatedNbRespawns",
                table: "ValidationResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValidatedScore",
                table: "ValidationResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValidatedTime",
                table: "ValidationResults",
                type: "int",
                nullable: true);
        }
    }
}
