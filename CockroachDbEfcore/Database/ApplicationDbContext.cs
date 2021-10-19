using CockroachDbEfcore.Entities;
using Microsoft.EntityFrameworkCore;

namespace CockroachDbEfcore.Database
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Item> Items { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Item>().ToTable("items");
            modelBuilder.Entity<Item>().HasKey(x => x.Key);
        }
    }
}