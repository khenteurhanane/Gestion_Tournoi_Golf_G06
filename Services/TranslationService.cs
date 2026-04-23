using System.Collections.Generic;

namespace croupe_06_TournoiGolf.Services
{
    public class TranslationService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["FR"] = new() {
                ["Home"] = "Accueil",
                ["Tournaments"] = "Tournois",
                ["Sponsor"] = "Commanditer",
                ["Shop"] = "Magasiner",
                ["Contact"] = "Contact",
                ["Login"] = "Connexion",
                ["Logout"] = "Déconnexion",
                ["Profile"] = "Mon profil",
                ["MyTeams"] = "Mes équipes",
                ["Inscriptions"] = "Mes inscriptions",
                ["Welcome"] = "Bienvenue",
                ["ExploreTournaments"] = "Explorer les tournois",
                ["RegisterNow"] = "S'inscrire maintenant"
            },
            ["EN"] = new() {
                ["Home"] = "Home",
                ["Tournaments"] = "Tournaments",
                ["Sponsor"] = "Sponsor",
                ["Shop"] = "Shop",
                ["Contact"] = "Contact",
                ["Login"] = "Login",
                ["Logout"] = "Logout",
                ["Profile"] = "My Profile",
                ["MyTeams"] = "My Teams",
                ["Inscriptions"] = "My Registrations",
                ["Welcome"] = "Welcome",
                ["ExploreTournaments"] = "Explore Tournaments",
                ["RegisterNow"] = "Register Now"
            },
            ["ES"] = new() {
                ["Home"] = "Inicio",
                ["Tournaments"] = "Torneos",
                ["Sponsor"] = "Patrocinar",
                ["Shop"] = "Tienda",
                ["Contact"] = "Contacto",
                ["Login"] = "Acceso",
                ["Logout"] = "Cerrar sesión",
                ["Profile"] = "Mi perfil"
            }
            // Ajoutez d'autres langues selon les besoins...
        };

        public string Get(string key, string lang)
        {
            lang = lang?.ToUpper() ?? "FR";
            if (!_translations.ContainsKey(lang)) lang = "FR";
            
            if (_translations[lang].TryGetValue(key, out var translation))
                return translation;

            // Retourne la clé par défaut si non trouvé
            return _translations["FR"].TryGetValue(key, out var fallback) ? fallback : key;
        }
    }
}
