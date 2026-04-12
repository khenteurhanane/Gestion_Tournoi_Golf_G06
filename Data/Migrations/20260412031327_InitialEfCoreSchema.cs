using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace croupe_06_TournoiGolf.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialEfCoreSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tournois",
                columns: table => new
                {
                    TournoiId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateTournoi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Lieu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InscriptionsOuvertes = table.Column<bool>(type: "bit", nullable: false),
                    PlacesParticipantsMax = table.Column<int>(type: "int", nullable: false),
                    NbEquipesMax = table.Column<int>(type: "int", nullable: false),
                    DateLimiteInscription = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreeLe = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournois", x => x.TournoiId);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    UtilisateurId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MotDePasseHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NomEntreprise = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateNaissance = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Adresse = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreeLe = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.UtilisateurId);
                });

            migrationBuilder.CreateTable(
                name: "Commandites",
                columns: table => new
                {
                    CommanditeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilisateurId = table.Column<int>(type: "int", nullable: false),
                    TournoiId = table.Column<int>(type: "int", nullable: false),
                    TypeCommandite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commandites", x => x.CommanditeId);
                    table.ForeignKey(
                        name: "FK_Commandites_Tournois_TournoiId",
                        column: x => x.TournoiId,
                        principalTable: "Tournois",
                        principalColumn: "TournoiId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Commandites_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "UtilisateurId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipes",
                columns: table => new
                {
                    EquipeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournoiId = table.Column<int>(type: "int", nullable: false),
                    NomEquipe = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CodeSecret = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NbJoueursMax = table.Column<int>(type: "int", nullable: false),
                    CreeParUtilisateurId = table.Column<int>(type: "int", nullable: false),
                    CreeLe = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipes", x => x.EquipeId);
                    table.ForeignKey(
                        name: "FK_Equipes_Tournois_TournoiId",
                        column: x => x.TournoiId,
                        principalTable: "Tournois",
                        principalColumn: "TournoiId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipes_Utilisateurs_CreeParUtilisateurId",
                        column: x => x.CreeParUtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "UtilisateurId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    ParticipantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournoiId = table.Column<int>(type: "int", nullable: false),
                    UtilisateurId = table.Column<int>(type: "int", nullable: true),
                    EquipeId = table.Column<int>(type: "int", nullable: true),
                    CommanditeId = table.Column<int>(type: "int", nullable: true),
                    Nom = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Prenom = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TypeParticipant = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatutInscription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MontantPaye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreeLe = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.ParticipantId);
                    table.ForeignKey(
                        name: "FK_Participants_Commandites_CommanditeId",
                        column: x => x.CommanditeId,
                        principalTable: "Commandites",
                        principalColumn: "CommanditeId");
                    table.ForeignKey(
                        name: "FK_Participants_Tournois_TournoiId",
                        column: x => x.TournoiId,
                        principalTable: "Tournois",
                        principalColumn: "TournoiId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participants_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "UtilisateurId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commandites_TournoiId",
                table: "Commandites",
                column: "TournoiId");

            migrationBuilder.CreateIndex(
                name: "IX_Commandites_UtilisateurId",
                table: "Commandites",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipes_CodeSecret",
                table: "Equipes",
                column: "CodeSecret",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipes_CreeParUtilisateurId",
                table: "Equipes",
                column: "CreeParUtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipes_TournoiId",
                table: "Equipes",
                column: "TournoiId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_CommanditeId",
                table: "Participants",
                column: "CommanditeId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_TournoiId_UtilisateurId",
                table: "Participants",
                columns: new[] { "TournoiId", "UtilisateurId" });

            migrationBuilder.CreateIndex(
                name: "IX_Participants_UtilisateurId",
                table: "Participants",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Email",
                table: "Utilisateurs",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Equipes");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "Commandites");

            migrationBuilder.DropTable(
                name: "Tournois");

            migrationBuilder.DropTable(
                name: "Utilisateurs");
        }
    }
}
