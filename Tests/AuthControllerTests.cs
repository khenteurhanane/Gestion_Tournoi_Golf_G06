using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using croupe_06_TournoiGolf.Controllers;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Models.ViewModels;
using croupe_06_TournoiGolf.Services;

namespace GolfTournoi.Tests
{
    public class AuthControllerTests
    {
        private GolfDbContext CreerContexte()
        {
            var options = new DbContextOptionsBuilder<GolfDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new GolfDbContext(options);
        }

        private AuthController CreerController(
            GolfDbContext context,
            IEmailService? emailService = null,
            IPasswordHasher? passwordHasher = null)
        {
            var controller = new AuthController(
                passwordHasher ?? new PasswordHasher(),
                context,
                emailService ?? new FakeEmailService());

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Request.Scheme = "https";

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
            controller.Url = new FakeUrlHelper();

            return controller;
        }

        [Fact]
        public async Task Register_ModeleValide_EnvoieEmailBienvenue()
        {
            using var context = CreerContexte();
            var emailService = new FakeEmailService();
            var controller = CreerController(context, emailService);

            var resultat = await controller.Register(new RegisterViewModel
            {
                Prenom = "Jean",
                Nom = "Dupont",
                Email = "jean.dupont@test.com",
                MotDePasse = "MotDePasse123!",
                ConfirmMotDePasse = "MotDePasse123!"
            });

            var redirection = Assert.IsType<RedirectToActionResult>(resultat);
            var utilisateur = Assert.Single(context.Utilisateurs);
            var email = Assert.Single(emailService.WelcomeEmails);

            Assert.Equal("Index", redirection.ActionName);
            Assert.Equal("Tournoi", redirection.ControllerName);
            Assert.Equal(utilisateur.Email, email.ToEmail);
            Assert.Equal("Jean Dupont", email.ToName);
        }

        [Fact]
        public async Task ForgotPassword_UtilisateurExistant_StockeHashEtEnvoieLienAvecTokenBrut()
        {
            using var context = CreerContexte();
            context.Utilisateurs.Add(new Utilisateur
            {
                Email = "reset@test.com",
                MotDePasseHash = new PasswordHasher().HashPassword("Ancien123!"),
                Prenom = "Marie",
                Nom = "Curie",
                Role = "PARTICIPANT"
            });
            context.SaveChanges();

            var emailService = new FakeEmailService();
            var controller = CreerController(context, emailService);

            var resultat = await controller.ForgotPassword(new ForgotPasswordViewModel
            {
                Email = "reset@test.com"
            });

            var redirection = Assert.IsType<RedirectToActionResult>(resultat);
            var utilisateur = Assert.Single(context.Utilisateurs);
            var email = Assert.Single(emailService.PasswordResetEmails);
            var resetUri = new Uri(email.ResetLink);
            var query = HttpUtility.ParseQueryString(resetUri.Query);
            var tokenEnvoye = query["token"];

            Assert.Equal("ForgotPasswordConfirmation", redirection.ActionName);
            Assert.False(string.IsNullOrWhiteSpace(tokenEnvoye));
            Assert.Equal("reset@test.com", query["email"]);
            Assert.NotNull(utilisateur.ResetPasswordToken);
            Assert.NotEqual(tokenEnvoye, utilisateur.ResetPasswordToken);
            Assert.True(utilisateur.ResetPasswordTokenExpiry > DateTime.Now.AddMinutes(55));
        }

        [Fact]
        public async Task ResetPassword_AvecTokenDuLien_ReinitialiseMotDePasseEtInvalideToken()
        {
            using var context = CreerContexte();
            var passwordHasher = new PasswordHasher();
            context.Utilisateurs.Add(new Utilisateur
            {
                Email = "reset2@test.com",
                MotDePasseHash = passwordHasher.HashPassword("Ancien123!"),
                Prenom = "Paul",
                Nom = "Martin",
                Role = "PARTICIPANT"
            });
            context.SaveChanges();

            var emailService = new FakeEmailService();
            var controller = CreerController(context, emailService, passwordHasher);

            await controller.ForgotPassword(new ForgotPasswordViewModel
            {
                Email = "reset2@test.com"
            });

            var email = Assert.Single(emailService.PasswordResetEmails);
            var resetUri = new Uri(email.ResetLink);
            var query = HttpUtility.ParseQueryString(resetUri.Query);
            var tokenEnvoye = query["token"]!;
            var emailUtilisateur = query["email"]!;

            var affichage = controller.ResetPassword(tokenEnvoye, emailUtilisateur);
            var vue = Assert.IsType<ViewResult>(affichage);
            var modele = Assert.IsType<ResetPasswordViewModel>(vue.Model);
            Assert.Equal(tokenEnvoye, modele.Token);

            var resultat = controller.ResetPassword(new ResetPasswordViewModel
            {
                Email = emailUtilisateur,
                Token = tokenEnvoye,
                NewPassword = "Nouveau123!",
                ConfirmPassword = "Nouveau123!"
            });

            var redirection = Assert.IsType<RedirectToActionResult>(resultat);
            var utilisateur = Assert.Single(context.Utilisateurs);

            Assert.Equal("Login", redirection.ActionName);
            Assert.Null(utilisateur.ResetPasswordToken);
            Assert.Null(utilisateur.ResetPasswordTokenExpiry);
            Assert.True(passwordHasher.VerifyPassword("Nouveau123!", utilisateur.MotDePasseHash));
        }

        private sealed class FakeEmailService : IEmailService
        {
            public List<(string ToEmail, string ToName, string ResetLink)> PasswordResetEmails { get; } = new();
            public List<(string ToEmail, string ToName)> WelcomeEmails { get; } = new();

            public Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
            {
                PasswordResetEmails.Add((toEmail, toName, resetLink));
                return Task.CompletedTask;
            }

            public Task SendWelcomeEmailAsync(string toEmail, string toName)
            {
                WelcomeEmails.Add((toEmail, toName));
                return Task.CompletedTask;
            }
        }

        private sealed class FakeUrlHelper : IUrlHelper
        {
            public ActionContext ActionContext { get; } = new();

            public string? Action(UrlActionContext actionContext)
            {
                var values = new RouteValueDictionary(actionContext.Values);
                var token = values["token"]?.ToString() ?? string.Empty;
                var email = values["email"]?.ToString() ?? string.Empty;
                var protocol = actionContext.Protocol ?? "https";

                return $"{protocol}://golf.test/{actionContext.Controller}/{actionContext.Action}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
            }

            public string? Content(string? contentPath) => contentPath;

            public bool IsLocalUrl(string? url) => true;

            public string? Link(string? routeName, object? values) => null;

            public string? RouteUrl(UrlRouteContext routeContext) => null;
        }
    }
}
