using Microsoft.EntityFrameworkCore;
using velotracker.Models;

namespace velotracker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Trail> Trails { get; set; }
        public DbSet<TrailPoint> TrailPoints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unikalnoœæ Emaili i nazw u¿ytkowników
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            //Relacja Trail -> User 
            modelBuilder.Entity<Trail>()
                .HasOne(r => r.User)
                .WithMany(u => u.Trails)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacja TrailPoint -> Trail
            modelBuilder.Entity<TrailPoint>()
                .HasOne(rp => rp.Trail)
                .WithMany(r => r.TrailPoints)
                .HasForeignKey(rp => rp.TrailId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}