using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// Add, update, delete and fetch key-value entries.
    /// </summary>
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

        /// <summary>
        /// Get a specific value by the key
        /// </summary>
        /// <param name="key" example="995c0628-3fb3-11ec-9356-0242ac130003">The max length is 256 characters</param>
        /// <param name="cancellationToken">Used to cancel the request</param>
        /// <response code="200" example="dmFsdWU=">Value retrieved</response>
        /// <response code="404">There is no entry for the key in the store</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAsync([FromRoute] string key, CancellationToken cancellationToken)
        {
            var item = await _dbContext.KeyValueEntries
                .FirstOrDefaultAsync(x => x.Key == Hash(key), cancellationToken);

            if (item == null) return NotFound();

            return new OkObjectResult(item.Value);
        }

        /// <summary>
        /// Upsert a key value entry.
        /// </summary>
        /// <remarks>
        /// The body of the request will be stored as the value for the "key"<br/><br/>
        /// <strong>NOTE:</strong>
        /// - The body must be a <see cref="!:https://en.wikipedia.org/wiki/Base64">base64</see> encoded string. <br/>
        /// - The body must be maximum 5kb long. ie. the length should be less than or equal to 5120 characters 
        /// </remarks>
        /// <param name="key" example="995c0628-3fb3-11ec-9356-0242ac130003">The max length is 256 characters</param>
        /// <param name="body" example="dmFsdWU=">The value encoded in base64. e.g the word "value" encoded in base64 is "dmFsdWU="</param>
        /// <param name="cancellationToken">Used to cancel the request</param>
        /// <response code="204">Value set successfully</response>
        /// <response code="400">Validation Error</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPut]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IList<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PutAsync(
            [FromRoute] string key,
            [FromBody] string body,
            CancellationToken cancellationToken
        )
        {
            var validationResult = await new ValueValidator().ValidateAsync(body, cancellationToken);
            if (!validationResult.IsValid)
                return new BadRequestObjectResult(validationResult.Errors.Select(x => x.ErrorMessage));

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

        /// <summary>
        /// Delete a key value entry.
        /// </summary>
        /// <param name="key" example="995c0628-3fb3-11ec-9356-0242ac130003">The max length is 256 characters</param>
        /// <param name="cancellationToken">Used to cancel the request</param>
        /// <response code="204">Value deleted successfully</response>
        /// <response code="404">There is no entry for the key in the store</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete]
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
            var byteArray = hash.ComputeHash(Encoding.UTF32.GetBytes(value));
            return Convert.ToBase64String(byteArray).ToLower();
        }
    }
}