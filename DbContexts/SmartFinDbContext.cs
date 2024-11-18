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

        }
    }
}