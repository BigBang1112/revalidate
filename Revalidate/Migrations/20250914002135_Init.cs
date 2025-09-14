using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revalidate.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Data = table.Column<byte[]>(type: "mediumblob", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Etag = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ValidationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Warnings = table.Column<string>(type: "JSON", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRequests", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ValidationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Sha256 = table.Column<byte[]>(type: "BINARY(32)", nullable: false),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplayId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    GhostId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsGhostExtracted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    GhostUid = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventsDuration = table.Column<int>(type: "int", nullable: false),
                    RaceTime = table.Column<int>(type: "int", nullable: true),
                    WalltimeStartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    WalltimeEndedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ExeVersion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExeChecksum = table.Column<uint>(type: "int unsigned", nullable: false),
                    OsKind = table.Column<int>(type: "int", nullable: false),
                    CpuKind = table.Column<int>(type: "int", nullable: false),
                    RaceSettings = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidationSeed = table.Column<int>(type: "int", nullable: true),
                    SteeringWheelSensitivity = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TitleId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleChecksum = table.Column<byte[]>(type: "BINARY(32)", nullable: true),
                    IsValid = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Problems = table.Column<string>(type: "JSON", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationResults_Files_GhostId",
                        column: x => x.GhostId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValidationResults_Files_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Files",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GhostCheckpoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<int>(type: "int", nullable: true),
                    StuntsScore = table.Column<int>(type: "int", nullable: true),
                    Speed = table.Column<float>(type: "float", nullable: true),
                    GhostId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhostCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GhostCheckpoints_ValidationResults_GhostId",
                        column: x => x.GhostId,
                        principalTable: "ValidationResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GhostInputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ValidationResultId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Time = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<int>(type: "int", nullable: true),
                    Pressed = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    X = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    Y = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    ValueF = table.Column<float>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhostInputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GhostInputs_ValidationResults_ValidationResultId",
                        column: x => x.ValidationResultId,
                        principalTable: "ValidationResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ValidationRequestEntityValidationResultEntity",
                columns: table => new
                {
                    RequestsId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ResultsId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRequestEntityValidationResultEntity", x => new { x.RequestsId, x.ResultsId });
                    table.ForeignKey(
                        name: "FK_ValidationRequestEntityValidationResultEntity_ValidationRequ~",
                        column: x => x.RequestsId,
                        principalTable: "ValidationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValidationRequestEntityValidationResultEntity_ValidationResu~",
                        column: x => x.ResultsId,
                        principalTable: "ValidationResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GhostCheckpoints_GhostId",
                table: "GhostCheckpoints",
                column: "GhostId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostInputs_ValidationResultId",
                table: "GhostInputs",
                column: "ValidationResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRequestEntityValidationResultEntity_ResultsId",
                table: "ValidationRequestEntityValidationResultEntity",
                column: "ResultsId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_GhostId",
                table: "ValidationResults",
                column: "GhostId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_ReplayId",
                table: "ValidationResults",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_Sha256",
                table: "ValidationResults",
                column: "Sha256");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GhostCheckpoints");

            migrationBuilder.DropTable(
                name: "GhostInputs");

            migrationBuilder.DropTable(
                name: "ValidationRequestEntityValidationResultEntity");

            migrationBuilder.DropTable(
                name: "ValidationRequests");

            migrationBuilder.DropTable(
                name: "ValidationResults");

            migrationBuilder.DropTable(
                name: "Files");
        }
    }
}
