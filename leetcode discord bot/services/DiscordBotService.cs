using Discord;
using Discord.WebSocket;
using Hangfire;
using leetcode_discord_bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Hosting;

namespace leetcode_discord_bot.services
{
    public class DiscordBotService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly LeetCodeService leetCodeService;
        private readonly IServiceScopeFactory serviceScopeFactory;
        public DiscordBotService(LeetCodeService _leetCodeService, IServiceScopeFactory serviceScopeFactory)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents =
                    GatewayIntents.Guilds |
                    GatewayIntents.GuildMessages |
                    GatewayIntents.MessageContent
            });

            _client.Log += LogAsync;
            _client.MessageReceived += HandleMessageAsync;
            this.serviceScopeFactory = serviceScopeFactory;
            leetCodeService = _leetCodeService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string token = "";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }
        public async Task StartAsync()
        {
            string token = "";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            var content = message.Content.Trim().ToLower();
            Console.WriteLine($"Received: {content}");

            if (content == "!ping ramen")
            {
                await message.Channel.SendMessageAsync("Pong! 🏓");
            }
            else if(message.Content.Contains("ramen addme"))
            {
                content = content.Replace("ramen addme", "").Trim();
                await addUser(message, content);
            }
            else if (message.Content.Contains("ramen dashboard"))
            {
                if (message.Channel is SocketTextChannel textChannel)
                {
                    ulong guild = textChannel.Guild.Id;
                
                    await dashboard(guild, message.Channel.Id);
                }
            }
            else if (message.Content.Contains("ramen tracehere"))
            {
                if (message.Channel is SocketTextChannel textChannel)
                {
                    ulong guild = textChannel.Guild.Id;

                    await Tracer(guild, message.Channel.Id);
                }
            }
            else if (message.Content.Contains("ramen nextproblemhere"))
            {
                if (message.Channel is SocketTextChannel textChannel)
                {
                    ulong guild = textChannel.Guild.Id;

                    await newproblem(guild, message.Channel.Id);
                }
            }
            else if (content == ("hello ramen"))
            {
                await message.Channel.SendMessageAsync($"Hello, {message.Author.Username}!");
            }
            else if (content == ("ramen"))
            {
                await message.Channel.SendMessageAsync($"Hello, {message.Author.Username} , did you just called ramen san");
            }

            
        }
        public async Task addUser(SocketMessage message,string content)
        {
            var scope =serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await db.Users.Where(u => u.discordId == message.Author.Id).FirstOrDefaultAsync();

            if (user == null)
            {
                user = new User
                {
                    discordId = message.Author.Id,
                    leetCodeUserName = content
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();    
            }
            else
            {
                user.leetCodeUserName = content;
                db.Users.Update(user); 
                await db.SaveChangesAsync();
            }

            var problemsSolved = await leetCodeService.GetTotalAcceptedCount(user.leetCodeUserName);

            await message.Channel.SendMessageAsync(
           $"Hello, <@{message.Author.Id}> , \n you have been assigned account {user.leetCodeUserName}\n with {problemsSolved} problems solved \n please if that not you re add your username using \n-> ramen addme (username)",
           allowedMentions: AllowedMentions.All);
        }
        public async Task dashboard(ulong serverId,ulong channelId)
        {
            if(_client.LoginState== LoginState.LoggedOut)
            {
                await StartAsync();
            }
            var scope = serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            //var guild = _client.GetGuild(serverId);
            var guild = await _client.Rest.GetGuildAsync(serverId);
            var channel =await (guild?.GetChannelAsync(channelId)) as ITextChannel;
           // var channel = await _client.Rest.GetChannelAsync(channelId);

            var users = await db.Users.Include(u => u.Problems).OrderBy(u => u.Problems.Count()).ToListAsync();

            var problemsSolved = await db.Problems.Where(p => p.solved == true).CountAsync();

            int per = 1000;
            int rank = 0;
            string message = $"**🚀  DASHBOARD 🚀**\n";
            message += $"total problems send **{problemsSolved}**\n";

            for (int i = 0; i < users.Count(); i++)
            {
                if (users[i].Problems.Count != per)
                {
                    rank++;
                    per = users[i].Problems.Count;
                    message += $"**================**\n";
                    message += $"{rank} place with {per} problems solved\n";
                }
                    message += $"<@{users[i].discordId}>\n";
            }
               await channel.SendMessageAsync(
                message,
              allowedMentions: AllowedMentions.All);

            BackgroundJob.Schedule(()=>dashboard(serverId,channelId),TimeSpan.FromDays(1));
        }
        public async Task Tracer(ulong serverId, ulong channelId)
        {
            if (_client.LoginState == LoginState.LoggedOut)
            {
                await StartAsync();
            }
            var scope = serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            //var guild = _client.GetGuild(serverId);
            var guild = await _client.Rest.GetGuildAsync(serverId);
            var channel = await (guild?.GetChannelAsync(channelId)) as ITextChannel;
            // var channel = await _client.Rest.GetChannelAsync(channelId);

            var users = await db.Users.Include(u => u.Problems).OrderBy(u => u.Problems.Count()).ToListAsync();

            var problems = await db.Problems.Where(p=>p.solved==true).ToListAsync();

            for (int i = 0; i < users.Count(); i++)
            {
                var unSolved = problems.Except(users[i].Problems).ToList()??new List<Problem>();
                var lastsolved = await leetCodeService.GetRecentSubmissionsAsState(users[i].leetCodeUserName);
                for (int o = 0; o < unSolved.Count; o++)
                {
                    if(lastsolved.Find(p=>p.ProblemId == unSolved[o].Leetcode)!=null)
                    {
                        users[i].Problems.Add(unSolved[o]);
                        string message = $"**🚀 <@{users[i].discordId}> solved {unSolved[o].Leetcode}  🚀**\n";
                        await channel.SendMessageAsync(
                        message,
                        allowedMentions: AllowedMentions.All);

                    }
                }
            }
            db.Users.UpdateRange(users);
            await db.SaveChangesAsync();

            BackgroundJob.Schedule(() => Tracer(serverId, channelId), TimeSpan.FromSeconds(60));
        }
        public async Task newproblem(ulong serverId, ulong channelId)
        {
            if (_client.LoginState == LoginState.LoggedOut)
            {
                await StartAsync();
            }
            var scope = serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            //var guild = _client.GetGuild(serverId);
            var guild = await _client.Rest.GetGuildAsync(serverId);
            var channel = await (guild?.GetChannelAsync(channelId)) as ITextChannel;
            // var channel = await _client.Rest.GetChannelAsync(channelId);


            var problem = await db.Problems.Where(p => p.solved == false).OrderBy(p=>p.Id).FirstOrDefaultAsync();
            if(problem ==null)
            {
                string message2 = $"**🚀 @everyone  we did it we solved 150 problems  🚀**\n";
                await channel.SendMessageAsync(
            message2,
            allowedMentions: AllowedMentions.All);
                return;
            }

            string message = $"**🚀 @everyone  new problem is here  🚀**\n";
            message += $"**🚀 [Problem Link](https://leetcode.com/problems/{problem.Leetcode}/description/) 🚀**\n";
            message += $"**🚀🚀🚀🚀🚀🚀🚀🚀🚀🚀🚀**\n";
            

            await channel.SendMessageAsync(
            message,
            allowedMentions: AllowedMentions.All);

            problem.solved = true;
            db.Problems.Update(problem);
            await db.SaveChangesAsync();

            BackgroundJob.Schedule(() => newproblem(serverId, channelId), TimeSpan.FromDays(1));
        }

        //        await message.Channel.SendMessageAsync(
        //   $"Hello, <@{message.Author.Id}>! Okay you are {content.Replace("ramen call me", "")}",
        //  allowedMentions: AllowedMentions.All);
    }
}
//
//await message.Channel.SendMessageAsync(
//    $"Hello, <@{message.Author.Id}>! Okay you are {content.Replace("ramen call me", "")}",
//    allowedMentions: AllowedMentions.Users(message.Author.Id)
//);



//            var scope =serviceScopeFactory.CreateScope();
// var db = scope . serviceprovider . get 
