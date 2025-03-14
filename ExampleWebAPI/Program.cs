using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Scalar.AspNetCore;
using Serilog;

namespace ExampleWebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
        
        // OpenTelemetry Logging (Uygulama loglarını OpenTelemetry ile entegre eder)
        builder.Logging.AddOpenTelemetry(options =>
        {
            // Log scope'larını (kapsamlarını) dahil eder. 
            // Örneğin, bir request sırasında oluşturulan kapsamlı logları takip etmeye yarar.
            options.IncludeScopes = true;

            // Log mesajlarının işlenmiş (formatlanmış) halini dahil eder.
            // Örneğin, içinde değişkenler olan bir log mesajını doğrudan kaydetmeyi sağlar.
            options.IncludeFormattedMessage = true;

            // Log state'lerini ayrıştırarak (parse ederek) OpenTelemetry'de kullanılabilir hale getirir.
            // Örneğin, structured logging formatında logları işlemeye yardımcı olur.
            options.ParseStateValues = true;
        });

        // OpenTelemetry yapılandırması
        builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProvider =>
        {
            tracerProvider
                .AddSource("ExampleWebAPI")
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("ExampleWebAPI")
                    .AddTelemetrySdk()
                    .AddEnvironmentVariableDetector())
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = httpContext =>
                    {
                        return !httpContext.Request.Path.Value?.Contains("health") ?? true;
                    };
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddConsoleExporter();
        })
        .WithMetrics(metrics =>
        {
            metrics
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyService"))
                .AddAspNetCoreInstrumentation() // HTTP request metrikleri
                .AddHttpClientInstrumentation() // HTTP client metrikleri
                .AddProcessInstrumentation() // İşlem bazlı metrikler (CPU, memory)
                .AddRuntimeInstrumentation() // .NET runtime metrikleri
                .AddMeter("System.Runtime") // .NET Runtime metrikleri için gerekli.. // GC süresi, bellek kullanımı, CPU yükü
                .AddMeter("System.Threading") // Thread havuzu metrikleri
                .AddEventCountersInstrumentation(options =>
                {
                    // HTTP istek ve bağlantı metrikleri
                    options.AddEventSources("System.Net.Http"); // HTTP istemci istek sayısı
                    options.AddEventSources("System.Net.Sockets"); // TCP bağlantıları
                    options.AddEventSources("System.Net.NameResolution"); // DNS istekleri
                    
                    // ASP.NET Core metrikleri
                    options.AddEventSources("Microsoft.AspNetCore.Hosting"); // Request süreleri
                    options.AddEventSources("Microsoft.AspNetCore.Http.Connections"); // WebSocket ve SignalR metrikleri
                })
                .AddConsoleExporter(); // Konsola yazdırma
        });
        // Metrics yapılandırması

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.UseSerilogRequestLogging();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(_ => _.Servers = []);
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapGet("/", () => Results.Redirect("/scalar"));
        app.MapGet("/hello", () => "Hello, World!");
        
        // Yeni information endpoint'i
        app.MapGet("/information", (ILogger<Program> logger) => {
            logger.LogInformation("/information endpoint'i çağrıldı");
            return "Information log message created";
        });

        // Yeni warning endpoint'i
        app.MapGet("/warning", (ILogger<Program> logger) => {
            logger.LogWarning("/warning endpoint'i çağrıldı");
            return "Warning log message created";
        });

        // Yeni error endpoint'i
        app.MapGet("/error", (ILogger<Program> logger) => {
            logger.LogError("/error endpoint'i çağrıldı");
            return "Error log message created";
        });

        app.Run();
    }
}
