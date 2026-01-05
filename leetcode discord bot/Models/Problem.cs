namespace leetcode_discord_bot.Models
{
    public class Problem
    {
        public int Id { get; set; }

        public string Leetcode { get; set; }

        public bool solved { get; set; }

        public List<User> Users { get; set; } 
    }
}
