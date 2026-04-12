using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace croupe_06_TournoiGolf.Data.Migrations
{
    /// <inheritdoc />
    public partial class GOLF143_ScoresTrous_EstEnCours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EstEnCours",
                table: "Tournois",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ScoresTrous",
                columns: table => new
                {
                    ScoreTrouId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipeId = table.Column<int>(type: "int", nullable: false),
                    TournoiId = table.Column<int>(type: "int", nullable: false),
                    NumeroTrou = table.Column<int>(type: "int", nullable: false),
                    NbCoups = table.Column<int>(type: "int", nullable: false),
                    SaisiLe = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoresTrous", x => x.ScoreTrouId);
                    table.ForeignKey(
                        name: "FK_ScoresTrous_Equipes_EquipeId",
                        column: x => x.EquipeId,
                        principalTable: "Equipes",
                        principalColumn: "EquipeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoresTrous_Tournois_TournoiId",
                        column: x => x.TournoiId,
                        principalTable: "Tournois",
                        principalColumn: "TournoiId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoresTrous_EquipeId",
                table: "ScoresTrous",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoresTrous_TournoiId",
                table: "ScoresTrous",
                column: "TournoiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoresTrous");

            migrationBuilder.DropColumn(
                name: "EstEnCours",
                table: "Tournois");
        }
    }
}
