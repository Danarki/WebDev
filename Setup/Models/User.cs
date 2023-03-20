using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using Microsoft.EntityFrameworkCore;
using WebDev.Controllers;

namespace WebDev.Models
{
    [Table("users")]
    public class User
    { 
        public int ID { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? VerifyToken { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? PasswordToken { get; set; }

        public void Insert(WebAppContext context)
        {
            context.Users.Add(this);
            context.SaveChanges();
        }
    }
}
