using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartFin.Entities;

namespace SmartFin.DbContexts
{

    public class SmartFinDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {

        public DbSet<Category> Categories { get; set; }

        public DbSet<Expense> Expenses { get; set; }

        public DbSet<Goal> Goals { get; set; }

        public DbSet<Remind> Reminds { get; set; }

        public SmartFinDbContext(DbContextOptions<SmartFinDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }
    }
}