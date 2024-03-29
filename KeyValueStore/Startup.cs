using System;
using System.IO;
using System.Reflection;
using Azure.Monitor.OpenTelemetry.Exporter;
using KeyValueStore.Database;
using KeyValueStore.Formatters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace KeyValueStore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenTelemetry().WithTracing(builder =>
            {
                var serviceName = Assembly.GetExecutingAssembly().GetName().Name;
                if (serviceName != null)
                    builder
                        .SetResourceBuilder(
                            ResourceBuilder.CreateDefault().AddService(serviceName)
                        )
                        .AddAspNetCoreInstrumentation()
                        .AddAzureMonitorTraceExporter(o =>
                            o.ConnectionString = Configuration.GetSection("APPLICATIONINSIGHTS_CONNECTION_STRING")
                                .Get<string>());
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString(nameof(ApplicationDbContext))));
            
            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>();

            services.AddControllers(o =>
            {
                o.InputFormatters.Insert(o.InputFormatters.Count, new TextPlainInputFormatter());
                o.OutputFormatters.Insert(o.OutputFormatters.Count, new TextPlainOutputFormatter());
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Key-Value Store API built with YugabyteDB",
                    Description = Configuration.GetSection("GIT_COMMIT").Get<string>(),
                    Contact = new OpenApiContact
                    {
                        Name = "Acho Arnold",
                        Email = "arnold@ndolestudio.com",
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://raw.githubusercontent.com/AchoArnold/key-value-store/main/LICENSE"),
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, true);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Key Value Store API v1");
                c.RoutePrefix = string.Empty;
                c.DocumentTitle = "Key-Value Store API built with YugabyteDB";
            });

            app.UseRouting();

            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}