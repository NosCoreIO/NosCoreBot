using System;
using System.Collections.Generic;
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
using Amazon.S3.Transfer;
using Discord.WebSocket;
using Newtonsoft.Json;
using NosCoreBot.Enumerations;
using NosCoreBot.Extensions;

namespace NosCoreBot.Modules
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        [Command("delete-message")]
        [Name("delete-message <amount>")]
        [Summary("Deletes a specified amount of messages")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Delete(int amount)
        {
            var messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
            await ((SocketTextChannel)Context.Channel).DeleteMessagesAsync(messages);
        }

        [Command("clear")]
        [Name("clear")]
        [Summary("clear all messages")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task Clear()
        {
            var clone = (Context.Channel as ITextChannel)?.CloneChannelAsync();
            if (clone != null)
            {
                await clone;
            }
        }

        [Command("reset-i18n")]
        [Name("reset-i18n")]
        [Summary("reset i18n file")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task ResetI18N()
        {
            var client = new AmazonS3Client(new BasicAWSCredentials(
                Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
                Environment.GetEnvironmentVariable("S3_SECRET_KEY")), RegionEndpoint.USWest2);
            var transferUtility = new TransferUtility(client);

            var newList = new Dictionary<RegionType, List<string>>();
            foreach (var type in Enum.GetValues(typeof(RegionType)).Cast<RegionType>())
            {
                newList.Add(type, new List<string>());
            }

            var emptyfile = JsonConvert.SerializeObject(newList);
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyfile)))
            {
                var transferUtilityUploadRequest = new TransferUtilityUploadRequest
                {
                    BucketName = "noscoretranslation",
                    Key = "missing_translation.json",
                    ContentType = "text/json",
                    InputStream = stream
                };

                await transferUtility.UploadAsync(transferUtilityUploadRequest);
            }
        }
    }
}
