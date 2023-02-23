using System.ComponentModel.DataAnnotations;
using WebDev.Controllers;

namespace WebDev.Models
{
    public class ContactForm
    {
        public int ID { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = null!;

        [Required]
        [StringLength(600)]
        public string Message { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        public void Insert()
        {
            var context = DatabaseController.Context;
            //context.Database.EnsureCreated();
            context.ContactForms.Add(this);
            context.SaveChanges();
        }
    }
}

