using System.ComponentModel.DataAnnotations;
using WebDev.Controllers;

namespace WebDev.Models
{
    public class GameRoom
    {
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public GameType Game { get; set; }
        public int GameID { get; set; }

        [Required]
        public int OwnerID { get; set; }

        public void Insert(WebAppContext context)
        {
            context.GameRooms.Add(this);
            context.SaveChanges();
        }

        public void Delete(WebAppContext context)
        {
            context.GameRooms.Remove(this);
            context.SaveChanges();
        }
    }
}
