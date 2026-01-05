namespace leetcode_discord_bot.Models
{
    public class User
    {
        public int Id { get; set; }
        public ulong discordId { get; set; }

        public string? leetCodeUserName { get; set; }

        public List<Problem> Problems { get; set; } =new List<Problem>();
    }
}
