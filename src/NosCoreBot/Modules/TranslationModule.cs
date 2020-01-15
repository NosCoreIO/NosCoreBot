using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Discord.WebSocket;
using Newtonsoft.Json;
using NosCoreBot.Enumerations;
using NosCoreBot.Extensions;
using Amazon;

namespace NosCoreBot.Modules
{
    public class TranslationModule : ModuleBase<SocketCommandContext>
    {
        [Command("reset-i18n")]
        [Name("reset-i18n")]
        [Summary("reset i18n file")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task ResetI18N()
        {
            using (var client = new AmazonS3Client(new BasicAWSCredentials(
                Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
                Environment.GetEnvironmentVariable("S3_SECRET_KEY")), RegionEndpoint.USWest2))
            {

                var newList = new Dictionary<RegionType, List<string>>();
                foreach (var type in Enum.GetValues(typeof(RegionType)).Cast<RegionType>())
                {
                    newList.Add(type, new List<string>());
                }

                var emptyfile = JsonConvert.SerializeObject(newList);
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyfile)))
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                        Key = Environment.GetEnvironmentVariable("S3_KEY"),
                        ContentType = "text/json",
                        InputStream = stream
                    };

                    await client.PutObjectAsync(putRequest);
                }
            }
        }

        [Command("translation")]
        [Name("translation <language>")]
        [Summary("Give the translator roles (You have to be on #translation-info)")]
        public async Task Translation(RegionType region)
        {
            if (Context.Channel.Name == "translation-info")
            {
                var role = Context.Guild.Roles.FirstOrDefault(r => r.Name == $"Translator({region.ToString()})");
                if (role == null)
                {
                    await ReplyAsync("the role doesn't exist.");
                    return;
                }

                var user = (SocketGuildUser)Context.User;
                if (role.Members.Any(r => r.Username == user.Username))
                {
                    await user.RemoveRoleAsync(role);
                    await ReplyAsync($"{user.Username} lost the role {role.Name}.");
                }
                else
                {
                    await user.AddRoleAsync(role);
                    await ReplyAsync($"{user.Username} got the role {role.Name}.");
                }
            }
        }
    }
}
