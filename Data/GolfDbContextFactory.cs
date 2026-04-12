using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace croupe_06_TournoiGolf.Data
{
    public class GolfDbContextFactory : IDesignTimeDbContextFactory<GolfDbContext>
    {
        public GolfDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("La chaîne de connexion 'DefaultConnection' est introuvable.");

            var optionsBuilder = new DbContextOptionsBuilder<GolfDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new GolfDbContext(optionsBuilder.Options);
        }
    }
}
