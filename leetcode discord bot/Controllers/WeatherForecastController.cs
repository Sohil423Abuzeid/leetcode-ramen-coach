using leetcode_discord_bot.Models;
using leetcode_discord_bot.services;
using Microsoft.AspNetCore.Mvc;

namespace leetcode_discord_bot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController(LeetCodeService leetCodeService,AppDbContext dbContext) : ControllerBase
    {
        
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        [HttpPost("addproblems")]
        public async Task<IActionResult> add(List<string> problems)
        {
            List<Problem> problemsList = new List<Problem>();
            foreach (var problemid in problems)
            {
                problemsList.Add(new Problem
                {
                    Leetcode = problemid
                });
            }
            dbContext.Problems.AddRange(problemsList);
            await dbContext.SaveChangesAsync();
            return Ok();
        }


        [HttpGet("aa")]
        public async Task<IActionResult> test()
        {
            return Ok(await leetCodeService.GetRecentSubmissionsAsState("Sohil423Abuzeid"));
        }
    }
}
