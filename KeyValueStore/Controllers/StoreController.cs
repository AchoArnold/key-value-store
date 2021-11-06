using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KeyValueStore.Database;
using KeyValueStore.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeyValueStore.Controllers
{
    [ApiController]
    [Route("store/{key}")]
    public class StoreController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public StoreController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync([FromRoute] string key, CancellationToken cancellationToken)
        {
            var item = await _dbContext.Store.FindAsync(new object[] { key }, cancellationToken);
            if (item == null) return NotFound();

            return new OkObjectResult(item);
        }

        [HttpPut]
        public async Task<IActionResult> PutAsync(
            [FromRoute] string key,
            CancellationToken cancellationToken
        )
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var item = new Store
                {
                    Key = key,
                    Value = await reader.ReadToEndAsync(),
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.Store
                    .Upsert(item)
                    .On(x => x.Key)
                    .RunAsync(cancellationToken);
            }

            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAsync(
            [FromRoute] string key,
            CancellationToken cancellationToken
        )
        {
            var item = await _dbContext.Store.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
            if (item == null)
            {
                return NotFound();
            }

            _dbContext.Store.Remove(item);

            return Ok();
        }
    }
}