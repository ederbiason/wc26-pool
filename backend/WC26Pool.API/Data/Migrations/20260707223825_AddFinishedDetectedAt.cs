using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WC26Pool.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinishedDetectedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinishedDetectedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishedDetectedAt",
                table: "Matches");
        }
    }
}
