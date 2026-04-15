using croupe_06_TournoiGolf.Services;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
 options.IdleTimeout = TimeSpan.FromMinutes(30);
 options.Cookie.HttpOnly = true;
 options.Cookie.IsEssential = true;
});

// Connexion à la base de données
builder.Services.AddDbContext<GolfDbContext>(options =>
 options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Service de hashage
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<TicketService>();

// SignalR pour le tableau de scores en temps réel (GOLF-143)
builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();

// Support multilingue (US-Localization)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews(options =>
{
 // Protection Anti-CSRF globale sur tous les POST (GOLF-132)
 options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
 .AddViewLocalization()
 .AddDataAnnotationsLocalization();

var app = builder.Build();

// Configuration des cultures supportées
var supportedCultures = new[] { "fr", "en" };

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

  // Crée la base et les tables basées sur les classes Models/ si elles n'existent pas encore
  context.Database.EnsureCreated();

  // --- REPARATION BASE DE DONNEES (GOLF-REPAIR) ---
  // On s'assure que les nouvelles tables de la boutique existent (Car EnsureCreated ne les ajoute pas si la DB existe déjà)
  context.Database.ExecuteSqlRaw(@"
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CommandesBoutique')
    BEGIN
        CREATE TABLE CommandesBoutique (
            CommandeId INT IDENTITY(1,1) PRIMARY KEY,
            UtilisateurId INT NULL,
            SousTotal DECIMAL(18,2) NOT NULL,
            Rabais DECIMAL(18,2) NOT NULL,
            Taxes DECIMAL(18,2) NOT NULL,
            TotalFinal DECIMAL(18,2) NOT NULL,
            ModePaiement NVARCHAR(50) NOT NULL,
            DateCommande DATETIME2 NOT NULL,
            CONSTRAINT FK_CommandesBoutique_Utilisateurs_UtilisateurId FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(UtilisateurId)
        );
    END

    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ItemsCommandesBoutique')
    BEGIN
        CREATE TABLE ItemsCommandesBoutique (
            ItemId INT IDENTITY(1,1) PRIMARY KEY,
            CommandeId INT NOT NULL,
            ArticleId INT NOT NULL,
            ArticleNom NVARCHAR(100) NOT NULL,
            PrixUnitaire DECIMAL(18,2) NOT NULL,
            Quantite INT NOT NULL,
            CONSTRAINT FK_ItemsCommandesBoutique_CommandesBoutique_CommandeId FOREIGN KEY (CommandeId) REFERENCES CommandesBoutique(CommandeId) ON DELETE CASCADE
        );
    END

    -- Correction GOLF143 (EstEnCours dans Tournois)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tournois') AND name = 'EstEnCours')
    BEGIN
        ALTER TABLE Tournois ADD EstEnCours BIT NOT NULL DEFAULT 0;
    END

    -- Ajout table Notifications
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
    BEGIN
        CREATE TABLE Notifications (
            NotificationId INT IDENTITY(1,1) PRIMARY KEY,
            Titre NVARCHAR(100) NOT NULL,
            Message NVARCHAR(MAX) NOT NULL,
            DateCreation DATETIME2 NOT NULL,
            EstLu BIT NOT NULL DEFAULT 0
        );
    END
  ");
  // ------------------------------------------------

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
    CreeLe = DateTime.Now
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
