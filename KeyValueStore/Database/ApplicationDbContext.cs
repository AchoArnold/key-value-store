using KeyValueStore.Entities;
using Microsoft.EntityFrameworkCore;

namespace KeyValueStore.Database
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Store> Store { get; set; } = default!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Store>().ToTable("store");
            modelBuilder.Entity<Store>().HasKey(x => x.Key);
        }
    }
}