using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using KeyValueStore.Database;
using KeyValueStore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace KeyValueStore.IntegrationTests
{
    [TestFixture]
    public class KeyValueStoreTests
    {
        [Test]
        public async Task Get_KeyExists_ValueReturned()
        {
            // Arrange
            var faker = new Faker();
            var key = faker.Random.String(faker.Random.Number(1, 256));
            var value = EncodeBase64(faker.Random.String(faker.Random.Number(1, 960)));
            var entries = new List<KeyValueEntry>{ new()
            {
                Key = Hash(key),
                Value = value,
                CreatedAt = DateTime.UtcNow
            }};
            
            var factory = new KeyValueStoreApplicationFactory(entries);
            var httpClient = factory.CreateClient();
            
            // Act
            var response = await httpClient.GetAsync($"v1/store/{key}");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be(value);
        }
        
        [Test]
        public async Task Get_KeyDoesNotExist_NotFoundReturned()
        {
            // Arrange
            var faker = new Faker();
            var key = faker.Random.String(faker.Random.Number(1, 256));
            var factory = new KeyValueStoreApplicationFactory(new List<KeyValueEntry>());
            var httpClient = factory.CreateClient();
            
            // Act
            var response = await httpClient.GetAsync($"v1/store/{key}");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().BeEmpty();
        }
        
        [Test]
        public async Task Put_KeyDoesNotExist_ValueSaved()
        {
            // Arrange
            var faker = new Faker();
            var key = faker.Random.String(faker.Random.Number(1, 256));
            var value = EncodeBase64(faker.Random.String(faker.Random.Number(1, 960)));
            var factory = new KeyValueStoreApplicationFactory(new List<KeyValueEntry>());
            var httpClient = factory.CreateClient();
            
            // Act
            var response = await httpClient.PutAsync($"v1/store/{key}", new StringContent(value));
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().BeEmpty();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entry = await db.KeyValueEntries.FirstOrDefaultAsync(x => x.Key == Hash(key));
            entry.Should().NotBeNull();
            entry.Value.Should().Be(value);
        }

        [TestCaseSource(nameof(GenerateInvalidValues))]
        public async Task Put_InvalidValue_BadRequestReturned(string value)
        {
            // Arrange
            var faker = new Faker();
            var key = faker.Random.String(faker.Random.Number(1, 256));
            var factory = new KeyValueStoreApplicationFactory(new List<KeyValueEntry>());
            var httpClient = factory.CreateClient();
            
            // Act
            var response = await httpClient.PutAsync($"v1/store/{key}", new StringContent(value));
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entry = await db.KeyValueEntries.FirstOrDefaultAsync(x => x.Key == Hash(key));
            entry.Should().BeNull();
        }
        
        [Test]
        public async Task Put_KeyExists_ValueUpdated()
        {
            // Arrange
            var faker = new Faker();
            var key = faker.Random.String(faker.Random.Number(1, 256));
            var oldValue = EncodeBase64(faker.Random.String(faker.Random.Number(1, 960)));
            var value = EncodeBase64(faker.Random.String(faker.Random.Number(1, 960)));
            var factory = new KeyValueStoreApplicationFactory(new List<KeyValueEntry>
            {
                new()
                {
                    Key = Hash(key),
                    Value = EncodeBase64(oldValue),
                    CreatedAt = DateTime.UtcNow
                }
            });
            
            var httpClient = factory.CreateClient();
            
            // Act
            var response = await httpClient.PutAsync($"v1/store/{key}", new StringContent(value));
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().BeEmpty();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entry = await db.KeyValueEntries.FirstOrDefaultAsync(x => x.Key == Hash(key));
            entry.Should().NotBeNull();
            entry.Value.Should().Be(value);
        }

        [Test]
        public async Task Delete_KeyExists_ValueDeleted()
        {
            // Arrange
            var faker = new Faker();
            var key = faker.Random.String(faker.Random.Number(1, 256));
            var value = EncodeBase64(faker.Random.String(faker.Random.Number(1, 960)));
            
            var entries = new List<KeyValueEntry>{ new()
            {
                Key = Hash(key),
                Value = EncodeBase64(value),
                CreatedAt = DateTime.UtcNow
            }};
            
            var factory = new KeyValueStoreApplicationFactory(entries);
            var httpClient = factory.CreateClient();
            
            // Act
            var response = await httpClient.DeleteAsync($"v1/store/{key}");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().BeNullOrEmpty();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entry = await db.KeyValueEntries.FirstOrDefaultAsync(x => x.Key == Hash(key));
            entry.Should().BeNull();
        }
        
        [Test]
        public async Task Delete_KeyDoesNotExist_NotFoundReturned()
        {
            // Arrange
            var faker = new Faker();
            var key = faker.Random.String(faker.Random.Number(1, 256));
            var factory = new KeyValueStoreApplicationFactory(new List<KeyValueEntry>());
            var httpClient = factory.CreateClient();
            
            // Act
            var response = await httpClient.DeleteAsync($"v1/store/{key}");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().BeEmpty();
        }
        
        private static IEnumerable<string> GenerateInvalidValues()
        {
            var faker = new Faker();
            yield return "";
            yield return faker.Random.String(faker.Random.Number(5121, 10000));
            yield return EncodeBase64(faker.Random.String(faker.Random.Number(5121, 10000)));

        }

        private static string EncodeBase64(string value) => Convert.ToBase64String(Encoding.UTF32.GetBytes(value));

        private static string Hash(string value)
        {
            using var hash = SHA256.Create();
            var byteArray = hash.ComputeHash( Encoding.UTF32.GetBytes( value ) );
            return Convert.ToBase64String(byteArray).ToLower();
        }
    }
}