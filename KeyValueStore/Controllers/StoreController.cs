using System;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KeyValueStore.Database;
using KeyValueStore.Entities;
using KeyValueStore.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeyValueStore.Controllers
{
    [ApiController]
    [Route("v1/store/{key:maxlength(256):minlength(1)}")]
    [Consumes(MediaTypeNames.Text.Plain)]
    [Produces(MediaTypeNames.Text.Plain)]
    public class StoreController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public StoreController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAsync([FromRoute] string key, CancellationToken cancellationToken)
        {
            var item = await _dbContext.KeyValueEntries
                .FirstOrDefaultAsync(x => x.Key == Hash(key), cancellationToken);
            
            if (item == null) return NotFound();
            
            return new OkObjectResult(item.Value);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> PutAsync(
            [FromRoute] string key,
            [FromBody] string body,
            CancellationToken cancellationToken
        )
        {
            var validationResult = await new ValueValidator().ValidateAsync(body, cancellationToken);
            if (!validationResult.IsValid) return new BadRequestObjectResult(validationResult.Errors);

            var item = new KeyValueEntry
            {
                Key = Hash(key),
                Value = body,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.KeyValueEntries
                .Upsert(item)
                .On(x => x.Key)
                .RunAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAsync(
            [FromRoute] string key,
            CancellationToken cancellationToken
        )
        {
            var item = await _dbContext.KeyValueEntries
                .FirstOrDefaultAsync(x => x.Key == Hash(key), cancellationToken);
            
            if (item == null) return NotFound();

            _dbContext.KeyValueEntries.Remove(item);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        private static string Hash(string value)
        {
            using var hash = SHA256.Create();
            var byteArray = hash.ComputeHash( Encoding.UTF32.GetBytes( value ) );
            return Convert.ToBase64String(byteArray).ToLower();
        }
    }
}