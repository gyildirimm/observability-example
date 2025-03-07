using Scalar.AspNetCore;
using Serilog;

namespace ExampleWebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
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
