using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WC26Pool.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPickemTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickemBracketSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    TeamFlag = table.Column<string>(type: "text", nullable: false),
                    IsEliminated = table.Column<bool>(type: "boolean", nullable: false),
                    EliminatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickemBracketSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PickemEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipantId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickemEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickemEntries_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickemStandings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipantId = table.Column<int>(type: "integer", nullable: false),
                    TotalPickemPoints = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickemStandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickemStandings_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickemPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PickemEntryId = table.Column<int>(type: "integer", nullable: false),
                    Round = table.Column<string>(type: "text", nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    ChosenTeam = table.Column<string>(type: "text", nullable: false),
                    ChosenTeamFlag = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    PointsEarned = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickemPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickemPicks_PickemEntries_PickemEntryId",
                        column: x => x.PickemEntryId,
                        principalTable: "PickemEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickemEntries_ParticipantId",
                table: "PickemEntries",
                column: "ParticipantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickemPicks_PickemEntryId",
                table: "PickemPicks",
                column: "PickemEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PickemStandings_ParticipantId",
                table: "PickemStandings",
                column: "ParticipantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickemBracketSlots");

            migrationBuilder.DropTable(
                name: "PickemPicks");

            migrationBuilder.DropTable(
                name: "PickemStandings");

            migrationBuilder.DropTable(
                name: "PickemEntries");
        }
    }
}
