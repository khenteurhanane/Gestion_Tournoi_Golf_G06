using croupe_06_TournoiGolf.Services;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Hubs;
using croupe_06_TournoiGolf.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

// Compatibilité DateTime.Now avec PostgreSQL (Npgsql 6+)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSession(options =>
{
 options.IdleTimeout = TimeSpan.FromMinutes(30);
 options.Cookie.HttpOnly = true;
 options.Cookie.IsEssential = true;
});

// Connexion à la base de données (SQL Server en dev, PostgreSQL en production)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.IsProduction() && connectionString != null && connectionString.StartsWith("postgres"))
{
 var uri = new Uri(connectionString);
 var userInfo = uri.UserInfo.Split(':');
 var dbPort = uri.Port > 0 ? uri.Port : 5432;
 connectionString = $"Host={uri.Host};Port={dbPort};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]}";
}
builder.Services.AddDbContext<GolfDbContext>(options =>
{
 if (builder.Environment.IsProduction())
  options.UseNpgsql(connectionString);
 else
  options.UseSqlServer(connectionString);
});

// Service de hashage
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<TicketService>();

// Service de matchmaking pour remplir les équipes
builder.Services.AddScoped<MatchmakingService>();

// Service d'envoi d'emails (GOLF-131)
builder.Services.AddScoped<IEmailService, EmailService>();

// SignalR pour le tableau de scores en temps réel (GOLF-143)
builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();

// Services Météo
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<WeatherService>();

// Support multilingue (US-Localization)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews(options =>
{
 // Protection Anti-CSRF globale sur tous les POST (GOLF-132)
 options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
 .AddViewLocalization()
 .AddDataAnnotationsLocalization();

// Render fournit le port via la variable PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
 builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Configuration des cultures supportées
var supportedCultures = new[] { "fr", "en", "nl", "de", "es", "it", "sv" };

var localizationOptions = new RequestLocalizationOptions()
 .SetDefaultCulture("fr")
 .AddSupportedCultures(supportedCultures)
 .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
 app.UseExceptionHandler("/Home/Error");
 app.UseHsts();
}

if (!app.Environment.IsProduction())
 app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
 name: "default",
 pattern: "{controller=Home}/{action=Index}/{id?}");

// --- INITIALISATION DE LA BASE DE DONNÉES ---
// Cette étape assure que la base de données et les tables sont créées si elles n'existent pas.
// Si vous avez modifié vos modèles (Models), supprimez la base GolfTournoiDB dans SQLEXPRESS pour qu'elle soit recréée.
using (var scope = app.Services.CreateScope())
{
 var services = scope.ServiceProvider;
 try
 {
  var context = services.GetRequiredService<GolfDbContext>();

  // En production (PostgreSQL) : crée les tables depuis le modèle
  // En dev (SQL Server) : applique les migrations
  if (app.Environment.IsProduction())
   context.Database.EnsureCreated();
  else
   context.Database.Migrate();

  // --- SEEDING : AJOUT DE L'ADMIN PAR DÉFAUT ---
  if (!context.Utilisateurs.Any(u => u.Role == "ADMIN"))
  {
   var hasher = services.GetRequiredService<IPasswordHasher>();
   context.Utilisateurs.Add(new Utilisateur
   {
    Email = "admin@lacite.ca",
    MotDePasseHash = hasher.HashPassword("Admin123!"),
    Role = "ADMIN",
    Prenom = "Admin",
    Nom = "G06",
    Telephone = "514-000-0000",
    CreeLe = DateTime.Now,
    EmailVerifie = true
   });
   context.SaveChanges();
  }
 }
 catch (Exception ex)
 {
  var logger = services.GetRequiredService<ILogger<Program>>();
  logger.LogError(ex, "Une erreur est survenue lors de l'initialisation de la base de données.");
 }
}

// ---------------------------------------------
// Route du hub SignalR
app.MapHub<ScoreHub>("/scorehub");
app.Run();
