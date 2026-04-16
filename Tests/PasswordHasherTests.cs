using Xunit;
using croupe_06_TournoiGolf.Services;

namespace GolfTournoi.Tests
{
    public class PasswordHasherTests
    {
        private PasswordHasher _hasher;

        public PasswordHasherTests()
        {
            _hasher = new PasswordHasher();
        }

        // Test 1 : verifier que le hash n'est pas vide
        [Fact]
        public void HashPassword_RetourneUnHash()
        {
            string motDePasse = "MonMotDePasse123";

            string hash = _hasher.HashPassword(motDePasse);

            Assert.False(string.IsNullOrEmpty(hash));
        }

        // Test 2 : verifier que le hash n'est pas deterministe a cause du sel BCrypt
        [Fact]
        public void HashPassword_MemMotDePasse_HashDifferentsMaisVerifiables()
        {
            string motDePasse = "Test1234";

            string hash1 = _hasher.HashPassword(motDePasse);
            string hash2 = _hasher.HashPassword(motDePasse);

            Assert.NotEqual(hash1, hash2);
            Assert.True(_hasher.VerifyPassword(motDePasse, hash1));
            Assert.True(_hasher.VerifyPassword(motDePasse, hash2));
        }

        // Test 3 : verifier que 2 mots de passe differents donnent des hash differents
        [Fact]
        public void HashPassword_MotsDePasseDifferents_HashDifferents()
        {
            string hash1 = _hasher.HashPassword("MotDePasse1");
            string hash2 = _hasher.HashPassword("MotDePasse2");

            Assert.NotEqual(hash1, hash2);
        }

        // Test 4 : verifier que VerifyPassword marche bien
        [Fact]
        public void VerifyPassword_BonMotDePasse_RetourneTrue()
        {
            string motDePasse = "password123";
            string hash = _hasher.HashPassword(motDePasse);

            bool resultat = _hasher.VerifyPassword(motDePasse, hash);

            Assert.True(resultat);
        }

        // Test 5 : verifier que VerifyPassword refuse un mauvais mot de passe
        [Fact]
        public void VerifyPassword_MauvaisMotDePasse_RetourneFalse()
        {
            string hash = _hasher.HashPassword("bonMotDePasse");

            bool resultat = _hasher.VerifyPassword("mauvaisMotDePasse", hash);

            Assert.False(resultat);
        }
    }
}
