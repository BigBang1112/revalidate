using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEndedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "ValidationDistroResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "ValidationDistroResults",
                type: "datetime(6)",
                nullable: true);
        }
    }
}
