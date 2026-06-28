using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WC26Pool.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnockoutStageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PenaltyWinnerTeam",
                table: "Predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "Matches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PenaltyAwayScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PenaltyHomeScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegularTimeAwayScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegularTimeHomeScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PenaltyWinnerTeam",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PenaltyAwayScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PenaltyHomeScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RegularTimeAwayScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RegularTimeHomeScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Matches");
        }
    }
}
