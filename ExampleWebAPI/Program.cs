
using Scalar.AspNetCore;

namespace ExampleWebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

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

        app.Run();
    }
}
