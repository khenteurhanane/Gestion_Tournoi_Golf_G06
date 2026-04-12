using croupe_06_TournoiGolf.Services;
using croupe_06_TournoiGolf.Data;
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

// S'assurer que la base de données et les colonnes nécessaires existent
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<GolfDbContext>();
    
    // Créer la DB si elle n'existe pas
    context.Database.EnsureCreated();

    // Correction pour la table Participants (US-12)
    try
    {
        // Ajout de CommanditeId
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Participants') AND name = 'CommanditeId') ALTER TABLE Participants ADD CommanditeId INT NULL;");
        
        // Ajout de Nom, Prenom, Email pour les invités
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Participants') AND name = 'Nom') ALTER TABLE Participants ADD Nom NVARCHAR(60) NULL;");
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Participants') AND name = 'Prenom') ALTER TABLE Participants ADD Prenom NVARCHAR(60) NULL;");
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Participants') AND name = 'Email') ALTER TABLE Participants ADD Email NVARCHAR(150) NULL;");
        
        // Correction pour Utilisateurs (NomEntreprise)
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Utilisateurs') AND name = 'NomEntreprise') ALTER TABLE Utilisateurs ADD NomEntreprise NVARCHAR(100) NULL;");

        // Ajout de ImageUrl pour les tournois
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tournois') AND name = 'ImageUrl') ALTER TABLE Tournois ADD ImageUrl NVARCHAR(300) NULL;");

        // Ajout de NbEquipesMax pour les tournois (valeur par défaut : 10)
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tournois') AND name = 'NbEquipesMax') ALTER TABLE Tournois ADD NbEquipesMax INT NOT NULL DEFAULT 10;");

        // Ajout de DateLimiteInscription pour les tournois
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tournois') AND name = 'DateLimiteInscription') ALTER TABLE Tournois ADD DateLimiteInscription DATETIME2 NULL;");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Erreur lors de la mise à jour des colonnes : " + ex.Message);
    }
}

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

app.Run();
