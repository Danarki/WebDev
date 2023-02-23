using WebDev.Models;
using Microsoft.EntityFrameworkCore;

namespace WebDev
{
    public class WebAppContext : DbContext
    {
        public WebAppContext(DbContextOptions<WebAppContext> options)
            : base(options)
        {
        }
        public DbSet<ContactForm> ContactForms { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContactForm>().ToTable("ContactForm");
            modelBuilder.Entity<User>().ToTable("User");

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=DESKTOP-DAAN;Database=cardigo;Trusted_Connection=True;Encrypt=False;");

            base.OnConfiguring(optionsBuilder);
        }
    }
}
