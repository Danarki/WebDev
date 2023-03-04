namespace WebDev.Models
{
    public enum GameTypeEnum
    {
        Dealer = 'D',
        GroupGame = 'G'
    }
    public class GameType
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public GameTypeEnum Type { get; set; }
        public int MaxPlayers { get; set; }
    }
}
