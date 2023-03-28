namespace WebDev.Models.ViewModels
{
    public class PlayerScoreDataViewModel
    {
        public string Name { get; set; }
        public int UserID { get; set; }
        public int HandScore { get; set; }
        public int GameScore { get; set; }
        public List<DeckCards> Cards { get; set; }
    }
}
