using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class AddMapId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MapId",
                table: "Maps",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapId",
                table: "Maps");
        }
    }
}
