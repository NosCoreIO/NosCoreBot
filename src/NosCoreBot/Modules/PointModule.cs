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

        public short DonationPoint { get; set; }

        public short TranslationPoint { get; set; }

        public short ContributionPoint { get; set; }
    }

    public class PointModule : ModuleBase<SocketCommandContext>
    {
        [Command("add-points")]
        [Name("add-points <user> <type> <amount>")]
        [Summary("add points to a user")]
        [RequireUserPermissionOrWebhook(GuildPermission.ManageMessages, new[] { "Travis CI" })]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task AddPoints(SocketGuildUser user, PointType type, int amount)
        {
            await ShowLeaderboard(user);
        }

        [Command("info-points")]
        [Name("info-points")]
        [Summary("show points")]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task ShowPoints()
        {
            await ShowLeaderboard((SocketGuildUser) Context.User);
        }

        [Command("leaderboard")]
        [Name("leaderboard <user?>")]
        [Summary("show leaderboard")]
        [RequireUserPermissionOrWebhook(GuildPermission.Administrator, new[] { "Travis CI" })]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task ShowLeaderboard(SocketGuildUser user = null)
        {
            //todo fetch list build it
            var users = new List<User>();
            if (user != null)
            {
                //todo look if user exist
                users.Add(new User { Username = user.Username });
            }

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Point Leaderboard");
            var rank = 1; //todo get real rank if user !=null
            foreach (var userFromList in users)
            {
                builder.AddField("Username", userFromList.Username, true);
                builder.AddField("Donation Translation Contribution", $@"{userFromList.DonationPoint.ToString().PadRight(8, '\u2000')} {userFromList.TranslationPoint.ToString().PadRight(9, '\u2000')} {userFromList.ContributionPoint}", true);
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
