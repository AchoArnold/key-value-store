using System.Collections.Generic;
using System.Linq;
using KeyValueStore.Database;
using KeyValueStore.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KeyValueStore.IntegrationTests
{
    public class KeyValueStoreApplicationFactory: WebApplicationFactory<Startup>
    {
        private readonly IList<KeyValueEntry> _keyValueEntries;

        public KeyValueStoreApplicationFactory(IList<KeyValueEntry> keyValueEntries)
        {
            _keyValueEntries = keyValueEntries;
        }
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // remove the existing context configuration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("application-db");
                });
                
                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                db.Database.EnsureCreated();

                db.KeyValueEntries.AddRange(_keyValueEntries);
                db.SaveChanges();
            });
            
            builder.UseSetting("https_port", "8080");
        }
    }
}