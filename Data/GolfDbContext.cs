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
    }
}
