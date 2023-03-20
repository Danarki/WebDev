using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;

namespace WebDev.Models
{
    [Table("connecteduser")]
    public class ConnectedUser
    {
        public int ID { get; set; }

        [AllowNull]
        public int RoomID { get; set; }

        [Required]
        public int UserID { get; set; }

        [AllowNull]
        public int GameID { get; set; }

        public string AuthToken { get; set; }

        public bool IsDisabled { get; set; }

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
