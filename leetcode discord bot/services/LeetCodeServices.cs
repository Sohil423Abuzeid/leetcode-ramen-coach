using Hangfire.Storage;
using leetcode_discord_bot.Dtos;
using System.Net.Http;
using System.Text;
using System.Text.Json;


namespace leetcode_discord_bot.services
{
    public class LeetCodeService
    {


public async Task<List<StateDto>> GetRecentSubmissionsAsState(string username, int limit = 50)
    {
        // GraphQL query
        var query = new
        {
            query = @"
        query recentSubmissions($username: String!, $limit: Int!) {
          recentAcSubmissionList(username: $username, limit: $limit) {
            titleSlug
            timestamp
            statusDisplay
          }
        }",
            variables = new { username, limit }
        };

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var content = new StringContent(
            JsonSerializer.Serialize(query),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync("https://leetcode.com/graphql", content);
        var json = await response.Content.ReadAsStringAsync();

        // Deserialize only the parts we need
        using var doc = JsonDocument.Parse(json);
        var list = new List<StateDto>();

        if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
            dataElement.TryGetProperty("recentAcSubmissionList", out var submissions))
        {
            foreach (var sub in submissions.EnumerateArray())
            {
                    long timestamp = 0;
                    var tsProp = sub.GetProperty("timestamp");

                    if (tsProp.ValueKind == JsonValueKind.Number)
                        timestamp = tsProp.GetInt64();
                    else if (tsProp.ValueKind == JsonValueKind.String)
                        timestamp = long.Parse(tsProp.GetString());

                    list.Add(new StateDto
                    {
                        ProblemId = sub.GetProperty("titleSlug").GetString(),
                        SubmissionTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime,
                        Status = sub.GetProperty("statusDisplay").GetString()
                    });
                }
        }

        return list;
    }



        public async Task<int> GetTotalAcceptedCount(string username)
        {
            var query = new
            {
                query = @"
        query userSolvedStats($username: String!) {
          matchedUser(username: $username) {
            submitStats {
              acSubmissionNum {
                difficulty
                count
              }
            }
          }
        }",
                variables = new { username }
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var content = new StringContent(
                JsonSerializer.Serialize(query),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://leetcode.com/graphql", content);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            var stats = doc.RootElement
                .GetProperty("data")
                .GetProperty("matchedUser")
                .GetProperty("submitStats")
                .GetProperty("acSubmissionNum");

            foreach (var item in stats.EnumerateArray())
            {
                if (item.GetProperty("difficulty").GetString() == "All")
                    return item.GetProperty("count").GetInt32();
            }

            return 0;
        }


    }
}
