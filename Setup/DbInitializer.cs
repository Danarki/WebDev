using Microsoft.EntityFrameworkCore;
using WebDev.Models;

namespace WebDev
{
    public class DbInitializer
    {
        public static void Initialize(WebAppContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any())
            {
                return;
            }

            User user = new User();
            user.Username = "admin";
            user.Password = BCrypt.Net.BCrypt.HashPassword("admin");
            user.VerifiedAt = DateTime.UtcNow; 
            user.Email = "admin@cardigo.com";

            context.Users.Add(user);

            try
            {
                context.SaveChanges();

            }
            catch (Exception e)
            {
                throw new Exception("Database Seeding Error!", e.InnerException);
            }
        }
    }
}
