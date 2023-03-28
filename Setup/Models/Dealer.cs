namespace WebDev.Models
{
    public class Dealer
    {
        public int ID { get; set; }
        public int GameID { get; set; }
        public bool? HasAce { get; set; }
        public int? HandScore { get; set; }
        public int? GameScore { get; set; }

    }
}
