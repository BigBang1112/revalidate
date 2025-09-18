using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class MapSha256Nullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Sha256",
                table: "Maps",
                type: "BINARY(32)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BINARY(32)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Sha256",
                table: "Maps",
                type: "BINARY(32)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "BINARY(32)",
                oldNullable: true);
        }
    }
}
