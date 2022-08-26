using System;
using Elastic.CommonSchema.Serilog;
using Horus.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;
using Serilog.Sinks.Elasticsearch;
using Elastic.Apm.NetCoreAll;
using Microsoft.Extensions.DependencyInjection;

namespace Horus.Extensions
{
    public static class SerilogExtension
    {
        public static IHostBuilder AddHorus(this IHostBuilder builder, IConfiguration configuration, string applicationName)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", $"{applicationName} - {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}")
                .Enrich.WithCorrelationId()
                .Enrich.WithExceptionDetails()
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.StaticFiles"))
                .WriteTo.Async(writeTo => writeTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(configuration["HorusConfiguration:ElasticsearchSettings:Uri"]))
                {
                    TypeName = null,
                    AutoRegisterTemplate = true,
                    IndexFormat = configuration["HorusConfiguration:ElasticsearchSettings:Index"],
                    BatchAction = ElasticOpType.Create,
                    CustomFormatter = new EcsTextFormatter(),
                    ModifyConnectionSettings = x => x.BasicAuthentication(configuration["HorusConfiguration:ElasticsearchSettings:Username"], configuration["HorusConfiguration:ElasticsearchSettings:Password"])
                }))
                .WriteTo.Async(writeTo => writeTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Message:lj} {Properties:j}{NewLine}{Exception}"))
                .CreateLogger();

            builder.ConfigureLogging(c => c.ClearProviders());
            builder.UseSerilog(Log.Logger, true);

            return builder;
        }

        public static IApplicationBuilder UseHorus(this IApplicationBuilder app, IConfiguration configuration)
        {
          
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseSerilogRequestLogging(opt =>
            {
                opt.EnrichDiagnosticContext = EnricherExtensions.EnrichFromRequest;
            });

            app.UseAllElasticApm(configuration);

            return app;
        }
    }
}
