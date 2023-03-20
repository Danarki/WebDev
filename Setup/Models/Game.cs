using System.ComponentModel.DataAnnotations.Schema;

namespace WebDev.Models
{
    [Table("game")]
    public class Game
    {
        public int ID { get; set; }
        public string GameID { get; set; }
    }
}
