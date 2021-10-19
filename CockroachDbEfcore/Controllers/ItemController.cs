using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CockroachDbEfcore.Database;
using CockroachDbEfcore.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CockroachDbEfcore.Controllers
{
    [ApiController]
    [Route("item/{key}")]
    public class ItemController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public ItemController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync([FromRoute] string key, CancellationToken cancellationToken)
        {
            var item = await _dbContext.Items.FindAsync(new object[] {key}, cancellationToken);
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
                var item = new Item
                {
                    Key = key,
                    Value = await reader.ReadToEndAsync(),
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.Items
                    .Upsert(item)
                    .On(x => x.Key)
                    .RunAsync(cancellationToken);
            }

            return Ok();
        }
    }
}