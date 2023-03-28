using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebDev.Controllers;

namespace WebDev.Models
{
    [Table("gameroom")]
    public class GameRoom
    {
        public int ID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        [Required]
        public bool HasStarted { get; set; }

        [Required]
        public int OwnerID { get; set; }

        [Required]
        public string OwnerToken { get; set; }

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
