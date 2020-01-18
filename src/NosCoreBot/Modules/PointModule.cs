using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Discord.WebSocket;
using Humanizer;
using Newtonsoft.Json;
using NosCoreBot.Enumerations;
using NosCoreBot.Extensions;
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
            var users = user == null ? await DownloadUsers() : await InitializeUser(user);
            var builder = new EmbedBuilder();
            builder.WithTitle("Point Leaderboard");
            var rank = 1; //todo get real rank if user !=null
            foreach (var userFromList in users)
            {
                builder.AddField("Username", userFromList.Username, true);
                builder.AddField("Donation Translation Contribution", $@"{userFromList.Points[PointType.DonationPoint].ToString().PadRight(8, '\u2000')} {userFromList.Points[PointType.TranslationPoint].ToString().PadRight(9, '\u2000')} {userFromList.Points[PointType.ContributionPoint]}", true);
                builder.AddField("Ranking",
                    (rank == 1 ? "🥇" :
                        (rank == 2 ? "🥈" :
                            (rank == 3 ? "🥉" : "")))
                        + rank.Ordinalize(new CultureInfo("en")), true);
                builder.WithColor(Color.Red);
                rank++;
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}
