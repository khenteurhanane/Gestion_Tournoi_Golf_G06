using BCrypt.Net;

namespace croupe_06_TournoiGolf.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // BCrypt gère automatiquement le sel et le hachage sécurisé
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 12);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // Vérifier si le mot de passe correspond au hash (gère les sels)
                return BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPassword);
            }
            catch (Exception)
            {
                // En cas de hash invalide ou ancien format
                return false;
            }
        }
    }
}
