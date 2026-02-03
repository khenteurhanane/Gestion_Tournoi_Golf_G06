namespace croupe_06_TournoiGolf.Models
{
    public class Participant
    {
        public int Id { get; set; }
        public string Prenom { get; set; }  
        public string Nom { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
        public string MotDePasseHash { get; set; }
    }
}
