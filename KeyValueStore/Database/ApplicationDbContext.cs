using KeyValueStore.Entities;
using Microsoft.EntityFrameworkCore;

namespace KeyValueStore.Database
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<KeyValueEntry> KeyValueEntries { get; set; } = default!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KeyValueEntry>().ToTable("store");
            modelBuilder.Entity<KeyValueEntry>().HasKey(x => x.Key);
        }
    }
}