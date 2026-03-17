using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Controllers;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;

namespace GolfTournoi.Tests
{
    public class TournoiControllerTests
    {
        // Configurer le controller avec une session admin simulée
        private TournoiController CreerControllerAvecSession(GolfDbContext context)
        {
            var controller = new TournoiController(context);

            // Créer un HttpContext avec une session en mémoire
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("UserRole", "ADMIN");
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetInt32("UserId", 1);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        [Fact]
        public void Create_ModeleValide_AjouteTournoiEtRedirige()
        {
            using (var context = CreerContexte())
            {
                var controller = CreerControllerAvecSession(context);
                var tournoi = new Tournoi
                {
                    Nom = "Open G06",
                    DateTournoi = DateTime.Today.AddDays(20),
                    Lieu = "Quebec"
                };

                var resultat = controller.Create(tournoi, null).Result;

                var redirection = Assert.IsType<RedirectToActionResult>(resultat);
                Assert.Equal("Index", redirection.ActionName);
                Assert.Equal(1, context.Tournois.Count());
            }
        }

        [Fact]
        public void Create_ModeleInvalide_RetourneVueSansSauvegarder()
        {
            using (var context = CreerContexte())
            {
                var controller = CreerControllerAvecSession(context);
                var tournoi = new Tournoi
                {
                    Nom = "Tournoi test",
                    DateTournoi = DateTime.Today.AddDays(20),
                    Lieu = "Montreal"
                };

                controller.ModelState.AddModelError("Nom", "Erreur test");

                var resultat = controller.Create(tournoi, null).Result;

                var vue = Assert.IsType<ViewResult>(resultat);
                Assert.Equal(0, context.Tournois.Count());
                Assert.Same(tournoi, vue.Model);
            }
        }

        [Fact]
        public void OuvrirInscriptions_MetLaValeurATrue()
        {
            using (var context = CreerContexte())
            {
                var tournoi = new Tournoi
                {
                    Nom = "Tournoi ouverture",
                    DateTournoi = DateTime.Today.AddDays(10),
                    Lieu = "Ottawa",
                    InscriptionsOuvertes = false
                };
                context.Tournois.Add(tournoi);
                context.SaveChanges();

                var controller = CreerControllerAvecSession(context);
                var resultat = controller.OuvrirInscriptions(tournoi.TournoiId);
                var redirection = Assert.IsType<RedirectToActionResult>(resultat);
                
                var tournoiMaj = context.Tournois.Find(tournoi.TournoiId);

                Assert.Equal("Index", redirection.ActionName);
                Assert.NotNull(tournoiMaj);
                Assert.True(tournoiMaj.InscriptionsOuvertes);
            }
        }

        [Fact]
        public void FermerInscriptions_MetLaValeurAFalse()
        {
            using (var context = CreerContexte())
            {
                var tournoi = new Tournoi
                {
                    Nom = "Tournoi fermeture",
                    DateTournoi = DateTime.Today.AddDays(10),
                    Lieu = "Ottawa",
                    InscriptionsOuvertes = true
                };
                context.Tournois.Add(tournoi);
                context.SaveChanges();

                var controller = CreerControllerAvecSession(context);
                var resultat = controller.FermerInscriptions(tournoi.TournoiId);
                var redirection = Assert.IsType<RedirectToActionResult>(resultat);
                var tournoiMaj = context.Tournois.Find(tournoi.TournoiId);

                Assert.Equal("Index", redirection.ActionName);
                Assert.NotNull(tournoiMaj);
                Assert.False(tournoiMaj.InscriptionsOuvertes);
            }
        }

        private GolfDbContext CreerContexte()
        {
            var options = new DbContextOptionsBuilder<GolfDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new GolfDbContext(options);
        }
    }

    // Session en mémoire pour les tests (remplace la session ASP.NET)
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new Dictionary<string, byte[]>();

        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _data.Keys;

        public void Clear() => _data.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _data.Remove(key);

        public void Set(string key, byte[] value) => _data[key] = value;

        public bool TryGetValue(string key, out byte[] value)
        {
            return _data.TryGetValue(key, out value!);
        }
    }
}
