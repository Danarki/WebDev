using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace WebDev.Models
{
    public enum GameTypeEnum
    {
        Dealer = 'D',
        GroupGame = 'G'
    }

    [Table("gametype")]
    public class GameType
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public GameTypeEnum Type { get; set; }
        public int MaxPlayers { get; set; }
        
        [AllowNull]
        public int MaxScore { get; set; }
    }
}
