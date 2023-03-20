using System.ComponentModel.DataAnnotations.Schema;

namespace WebDev.Models
{
    [Table("gamescore")]
    public class GameScore
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public int GameID { get; set; }
        public int Score { get; set; }
    }
}
