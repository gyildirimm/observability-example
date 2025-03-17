using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Scalar.AspNetCore;
using Serilog;
using OpenTelemetry.Exporter;
using System.Diagnostics;

namespace ExampleWebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
        
        // ActivitySource'u global olarak tanımla
        var activitySource = new ActivitySource("ExampleWebAPI", "1.0.0");

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
                .AddSource(activitySource.Name)
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("ExampleWebAPI")
                    .AddTelemetrySdk()
                    .AddEnvironmentVariableDetector())
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = null; // Tüm istekleri kaydet

                    // options.Filter = httpContext =>
                    // {
                    //     return !httpContext.Request.Path.Value?.Contains("health") ?? true;
                    // };
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddConsoleExporter()
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri("http://observability-tempo:4317"); // Tempo OTLP gRPC endpoint
                    opts.Protocol = OtlpExportProtocol.Grpc; // GRPC protokolünü kullan
                });
        })
        .WithMetrics(metrics =>
        {
            metrics
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyMetricService"))
                .AddAspNetCoreInstrumentation() // HTTP request metrikleri
                .AddHttpClientInstrumentation() // HTTP client metrikleri
                .AddProcessInstrumentation() // İşlem bazlı metrikler (CPU, memory)
                .AddRuntimeInstrumentation() // .NET runtime metrikleri
                .AddMeter("System.Runtime") // .NET Runtime metrikleri için gerekli.. // GC süresi, bellek kullanımı, CPU yükü
                .AddMeter("System.Threading") // Thread havuzu metrikleri
                .AddMeter("Custom.Http.Meter")
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
                .AddConsoleExporter() // Konsola yazdırma
                .AddPrometheusExporter(); // Prometheus exporter'ı ekle
        });
        // Metrics yapılandırması
        

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();
        app.UseSerilogRequestLogging(); // Serilog ile istek loglama

        app.AddCustomMetrics();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(_ => _.Servers = []);
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        // app.UseMetricServer();
        // app.UseHttpMetrics();
        // //app.MapMetrics();

        app.MapGet("/", () => Results.Redirect("/scalar"));
        app.MapGet("/hello", () => "Hello, World!");
        
        // Information endpoint'i
        app.MapGet("/information", (ILogger<Program> logger) => {
            using var activity = activitySource.StartActivity("Information Endpoint");
            activity?.SetTag("endpoint", "information");
            
            logger.LogInformation("/information endpoint'i çağrıldı");
            return "Information log message created";
        });

        // Warning endpoint'i
        app.MapGet("/warning", (ILogger<Program> logger) => {
            using var activity = activitySource.StartActivity("Warning Endpoint");
            activity?.SetTag("endpoint", "warning");
            activity?.SetTag("severity", "warning");
            
            logger.LogWarning("/warning endpoint'i çağrıldı");
            return "Warning log message created";
        });

        // Error endpoint'i
        app.MapGet("/error", (ILogger<Program> logger) => {
            using var activity = activitySource.StartActivity("Error Endpoint");
            activity?.SetTag("endpoint", "error");
            activity?.SetTag("severity", "error");
            
            logger.LogError("/error endpoint'i çağrıldı");
            return "Error log message created";
        });

        // Test trace endpoint'i (mevcut kodu güncelle)
        app.MapGet("/test-trace", async (ILogger<Program> logger) =>
        {
            using var activity = activitySource.StartActivity("Test Trace");
            activity?.SetTag("endpoint", "test-trace");
            activity?.SetTag("custom.tag", "value");
            
            logger.LogInformation("Test trace generated");
            
            await Task.Delay(500);
            return "Test trace sent!";
        });

        app.Run();
    }
}
