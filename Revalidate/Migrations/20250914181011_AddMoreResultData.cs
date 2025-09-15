using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreResultData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "Login",
                table: "ValidationResults",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MapUid",
                table: "ValidationResults",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ValidatedNbCheckpoints",
                table: "ValidationResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ValidatedNbRespawns",
                table: "ValidationResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ValidatedScore",
                table: "ValidationResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ValidatedTime",
                table: "ValidationResults",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "Login",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "MapUid",
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
        }
    }
}
