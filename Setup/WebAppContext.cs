using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using WebDev.Models;

namespace WebDev
{
    public class WebAppContext : DbContext
    {
        public DbSet<ContactForm> ContactForms { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=localhost;database=cardigo;user=root;password=");
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

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.Password).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordToken);
                entity.Property(e => e.VerifyToken);
                entity.Property(e => e.VerifiedAt);
            });
        }
    }
}
