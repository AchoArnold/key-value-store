using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace KeyValueStore.Formatters
{
    public class TextPlainOutputFormatter : OutputFormatter
    {
        private const string ContentType = MediaTypeNames.Text.Plain;

        public TextPlainOutputFormatter()
        {
            SupportedMediaTypes.Add(ContentType);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var content = string.Empty;
            if (context.Object is string contextObject)
            {
                content = contextObject;
            }
            return context.HttpContext.Response.WriteAsync(content);
        }
    }
}