
using Hangfire;
using leetcode_discord_bot.Models;
using leetcode_discord_bot.services;
using Microsoft.EntityFrameworkCore;

namespace leetcode_discord_bot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("Main")));

            builder.Services.AddHangfire(configuration =>
            configuration.UseSqlServerStorage(builder.Configuration.GetConnectionString("Main")));
            builder.Services.AddHangfireServer();
            // discord 
            builder.Services.AddHostedService<DiscordBotService>();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetIsOriginAllowed(_ => true);
                });
            });
            builder.Services.AddSingleton<LeetCodeService>();
            var app = builder.Build();
            app.UseCors();

            // Configure the HTTP request pipeline.
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHangfireDashboard("/hangfire");

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
