using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartFin.Entities;

namespace SmartFin.DbContexts
{

    public class SmartFinDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {

        public DbSet<Category> Categories { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Goal> Goals { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public SmartFinDbContext(DbContextOptions<SmartFinDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Many-to-Many связь между User и Goal
            modelBuilder.Entity<User>()
                .HasMany(e => e.Goals)
                .WithMany(e => e.Users);

            // One-to-Many связь между User и Transaction
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.user)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .IsRequired();

            // One-to-Many связь между User и Category
            modelBuilder.Entity<Category>()
                .HasOne(c => c.user)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .IsRequired();
        }
    }
}