using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Hugo.API.Hubs;
using Hugo.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Hugo.Core.Services;

namespace Hugo.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IGameService, GameService>();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins("http://localhost:5173")
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });

            // Add Infrastructure services
            builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCors();
            app.UseRouting();
            app.UseAuthorization();

            app.MapHub<GameHub>("/gameHub");
            app.MapControllers();

            // Default route'u ekledik
            app.MapFallbackToFile("index.html");

            app.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
