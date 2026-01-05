using Discord;
using Microsoft.EntityFrameworkCore;

namespace leetcode_discord_bot.Models
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<User> Users { get; set; }
        
        public DbSet<Problem> Problems { get; set; }
    }
}
