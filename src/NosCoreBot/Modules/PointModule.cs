using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ConsoleTableExt;
using Discord.WebSocket;
using Humanizer;
using Newtonsoft.Json;
using NosCoreBot.Precondition;

namespace NosCoreBot.Modules
{
    public enum PointType
    {
        DonationPoint,
        TranslationPoint,
        ContributionPoint
    }

    public class User
    {
        public string Username { get; set; }

        public Dictionary<PointType, int> Points { get; set; }
    }

    public class PointModule : ModuleBase<SocketCommandContext>
    {
        private async Task<List<User>> DownloadUsers()
        {
            var request = new GetObjectRequest
            {
                BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                Key = "contribution.json",
            };
            using (var client = new AmazonS3Client(new BasicAWSCredentials(
                Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
                Environment.GetEnvironmentVariable("S3_SECRET_KEY")), RegionEndpoint.USWest2))
            using (GetObjectResponse response = await client.GetObjectAsync(request))
            using (Stream responseStream = response.ResponseStream)
            using (StreamReader reader = new StreamReader(responseStream))
            {
                return JsonConvert.DeserializeObject<List<User>>(reader.ReadToEnd());
            }
        }

        private async Task UploadUsers(List<User> users)
        {
            using (var client = new AmazonS3Client(new BasicAWSCredentials(
                Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
                Environment.GetEnvironmentVariable("S3_SECRET_KEY")), RegionEndpoint.USWest2))
            {
                var emptyfile = JsonConvert.SerializeObject(users);
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyfile)))
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                        Key = "contribution.json",
                        ContentType = "text/json",
                        InputStream = stream
                    };

                    await client.PutObjectAsync(putRequest);
                }
            }
        }


        [Command("add-points")]
        [Name("add-points <user> <type> <amount>")]
        [Summary("add points to a user")]
        [RequireUserPermissionOrWebhook(GuildPermission.ManageMessages, new[] { "Travis CI" })]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task AddPoints(SocketGuildUser user, PointType type, int amount)
        {
            var points = await InitializeUser(user);
            var userinfo = points.FirstOrDefault(s => s.Username == user.Username);
            userinfo.Points[type] += amount;
            await UploadUsers(points);
            await ShowLeaderboard(user);
        }

        public async Task<List<User>> InitializeUser(SocketGuildUser user)
        {
            var users = await DownloadUsers();
            var userinfo = users.FirstOrDefault(s => s.Username == user.Username);
            if (userinfo == null)
            {
                userinfo = new User
                {
                    Username = user.Username,
                    Points = new Dictionary<PointType, int>
                    {
                        {PointType.DonationPoint, 0 },
                        {PointType.TranslationPoint, 0 },
                        {PointType.ContributionPoint, 0 }
                    }
                };
                users.Add(userinfo);
            }

            return users;
        }

        [Command("info-points")]
        [Name("info-points")]
        [Summary("show points")]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task ShowPoints()
        {
            await ShowLeaderboard((SocketGuildUser)Context.User);
        }

        [Command("leaderboard")]
        [Name("leaderboard <user?>")]
        [Summary("show leaderboard")]
        [RequireUserPermissionOrWebhook(GuildPermission.Administrator, new[] { "Travis CI" })]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task ShowLeaderboard(SocketGuildUser user = null)
        {
            var users = (user == null ? await DownloadUsers() : await InitializeUser(user)).OrderByDescending(s => s.Points[PointType.ContributionPoint] * 10 + s.Points[PointType.DonationPoint] * 5 + s.Points[PointType.TranslationPoint]).ToList();
            var leaderboard = (user == null ? users :
                users.Where(s => s.Username == user.Username));
            var rank = user == null ? 1 : users.FindIndex(s => s.Username == user.Username) + 1;
            var builder = new EmbedBuilder();

            builder.WithTitle("Point Leaderboard");
            var table = new DataTable();
            table.Columns.Add("Rank  ", typeof(string));
            table.Columns.Add("Username ", typeof(string));
            table.Columns.Add("Donation    ", typeof(int));
            table.Columns.Add("Translation ", typeof(int));
            table.Columns.Add("Contribution", typeof(int));

            foreach (var userFromList in leaderboard)
            {
                var ranking = (rank == 1 ? "🥇" :
                        (rank == 2 ? "🥈" :
                            (rank == 3 ? "🥉" : "\u2728")))
                    + rank.Ordinalize(new CultureInfo("en"));
                var username = $"**{userFromList.Username}**";
                var donationPoints = userFromList.Points[PointType.DonationPoint];
                var translationPoints = userFromList.Points[PointType.TranslationPoint];
                var contributionPoints = userFromList.Points[PointType.ContributionPoint];
                table.Rows.Add(ranking, username, donationPoints, translationPoints, contributionPoints);
                rank++;
                if (rank == 25)
                {
                    break;
                }
            }

            var text = ConsoleTableBuilder.From(table)
                .WithFormat(ConsoleTableBuilderFormat.Minimal)
                .WithOptions(new ConsoleTableBuilderOption { Delimiter = "",DividerString = "", TrimColumn = true})
                .Export().ToString();
            builder.AddField(text.Replace(" ", "\u2007\u2007").Split('\n')[0], string.Join('\n', text.Replace(' ', '\u2000').Split('\n').Skip(2)), true);
            builder.WithColor(Color.Red);
            builder.WithFooter("Note: The points on the leaderboard don't have the same value.\nContribution:x10 --- Donation:x5 --- Translation:x1");
            await ReplyAsync("", false, builder.Build());
        }
    }
}
