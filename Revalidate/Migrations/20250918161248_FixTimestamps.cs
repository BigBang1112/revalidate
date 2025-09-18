using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class FixTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ValidationResults");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ValidationResults");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "ValidationDistroResults",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "ValidationDistroResults",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "ValidationDistroResults",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ValidationDistroResults");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "ValidationDistroResults");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ValidationDistroResults");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "ValidationResults",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "ValidationResults",
                type: "datetime(6)",
                nullable: true);
        }
    }
}
