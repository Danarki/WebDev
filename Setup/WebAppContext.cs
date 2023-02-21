using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Setup.Models;

namespace Setup
{
    public class WebAppContext : DbContext
    {
        public DbSet<ContactForm> ContactForms { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=localhost;database=webdev;user=root;password=");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ContactForm>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Subject).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Email).IsRequired();
            });
        }
    }
}
