using croupe_06_TournoiGolf.Services;
using croupe_06_TournoiGolf.Data;
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
