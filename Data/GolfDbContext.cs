using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Data
{
    public class GolfDbContext : DbContext
    {
        public GolfDbContext(DbContextOptions<GolfDbContext> options) : base(options)
        {
        }

        // Tables de la base de données
        public DbSet<Tournoi> Tournois { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Equipe> Equipes { get; set; }
        public DbSet<Commandite> Commandites { get; set; }
        public DbSet<ScoreTrou> ScoresTrous { get; set; }
        public DbSet<CommandeBoutique> CommandesBoutique { get; set; }
        public DbSet<ItemCommandeBoutique> ItemsCommandesBoutique { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Index sur Email pour accélérer la recherche lors de l'authentification
            modelBuilder.Entity<Utilisateur>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Utilisateurs_Email");

            // Index sur CodeSecret pour la recherche rapide des équipes
            modelBuilder.Entity<Equipe>()
                .HasIndex(e => e.CodeSecret)
                .IsUnique()
                .HasDatabaseName("IX_Equipes_CodeSecret");

            // Index composé pour accélérer les recherches par tournoi et utilisateur
            // UtilisateurId est nullable pour les invités commandités
            modelBuilder.Entity<Participant>()
                .HasIndex(p => new { p.TournoiId, p.UtilisateurId })
                .HasDatabaseName("IX_Participants_TournoiId_UtilisateurId");
        }
    }
}
