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
                name: "ValidationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Log = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationLogs", x => x.Id);
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
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MapUid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sha256 = table.Column<byte[]>(type: "BINARY(32)", nullable: true),
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
                    UserUploaded = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MapId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
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

            migrationBuilder.CreateTable(
                name: "ValidationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Sha256 = table.Column<byte[]>(type: "BINARY(32)", nullable: false),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GameVersion = table.Column<int>(type: "int", nullable: false),
                    ReplayId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    GhostId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsGhostExtracted = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    NbInputs = table.Column<int>(type: "int", nullable: false),
                    Login = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MapUid = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MapId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ServerVersion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServerHostType = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsValid = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    IsValidExtracted = table.Column<bool>(type: "tinyint(1)", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_ValidationResults_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
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
                name: "ValidationDistroResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ResultId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DistroId = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    IsValidExtracted = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    DeclaredNbCheckpoints = table.Column<int>(type: "int", nullable: true),
                    DeclaredNbRespawns = table.Column<int>(type: "int", nullable: true),
                    DeclaredTime = table.Column<int>(type: "int", nullable: true),
                    DeclaredScore = table.Column<int>(type: "int", nullable: true),
                    ValidatedNbCheckpoints = table.Column<int>(type: "int", nullable: true),
                    ValidatedNbRespawns = table.Column<int>(type: "int", nullable: true),
                    ValidatedTime = table.Column<int>(type: "int", nullable: true),
                    ValidatedScore = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    InputsResult = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Desc = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawJsonResult = table.Column<string>(type: "longtext", maxLength: 32767, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationDistroResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationDistroResults_ValidationLogs_LogId",
                        column: x => x.LogId,
                        principalTable: "ValidationLogs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValidationDistroResults_ValidationResults_ResultId",
                        column: x => x.ResultId,
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

            migrationBuilder.CreateIndex(
                name: "IX_ValidationDistroResults_LogId",
                table: "ValidationDistroResults",
                column: "LogId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationDistroResults_ResultId",
                table: "ValidationDistroResults",
                column: "ResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRequestEntityValidationResultEntity_ResultsId",
                table: "ValidationRequestEntityValidationResultEntity",
                column: "ResultsId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_GhostId",
                table: "ValidationResults",
                column: "GhostId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_MapId",
                table: "ValidationResults",
                column: "MapId");

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
                name: "ValidationDistroResults");

            migrationBuilder.DropTable(
                name: "ValidationRequestEntityValidationResultEntity");

            migrationBuilder.DropTable(
                name: "ValidationLogs");

            migrationBuilder.DropTable(
                name: "ValidationRequests");

            migrationBuilder.DropTable(
                name: "ValidationResults");

            migrationBuilder.DropTable(
                name: "Maps");

            migrationBuilder.DropTable(
                name: "Files");
        }
    }
}
