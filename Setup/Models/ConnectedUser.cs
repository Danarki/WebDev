using Microsoft.Build.Framework;

namespace WebDev.Models
{
    public class ConnectedUser
    {
        public int ID { get; set; }

        [Required]
        public GameRoom Room { get; set; }
        public int RoomID { get; set; }

        [Required]
        public User User { get; set; }
        public int UserID { get; set; }

        public void Insert(WebAppContext context)
        {
            if (context.ConnectedUsers.Where(x => x.UserID == this.UserID && x.RoomID == this.RoomID).Any())
            {
                return;
            }

            context.ConnectedUsers.Add(this);
            context.SaveChanges();
        }

        public void Delete(WebAppContext context)
        {
            context.ConnectedUsers.Remove(this);
            context.SaveChanges();
        }
    }
}
