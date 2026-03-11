using croupe_06_TournoiGolf.Services;
using croupe_06_TournoiGolf.Data;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

// S'assurer que la base de données et les colonnes nécessaires existent
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<GolfDbContext>();
    
    // Créer la DB si elle n'existe pas
    context.Database.EnsureCreated();

    // Correction temporaire : s'assurer que la colonne NomEntreprise existe (migration manuelle simplifiée)
    try
    {
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Utilisateurs') AND name = 'NomEntreprise') ALTER TABLE Utilisateurs ADD NomEntreprise NVARCHAR(100) NULL;");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Erreur lors de la vérification de la colonne NomEntreprise : " + ex.Message);
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
