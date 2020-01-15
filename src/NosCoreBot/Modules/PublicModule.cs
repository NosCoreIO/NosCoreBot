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
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Discord.WebSocket;
using Newtonsoft.Json;
using NosCoreBot.Enumerations;
using NosCoreBot.Extensions;
using NosCoreBot.Precondition;

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
        [RequireUserPermissionOrWebhook(GuildPermission.Administrator, new[] { "Travis CI" })]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task Clear()
        {
            var user = Context.User as IUser;
            await ReplyAsync($"user is webhook : {user.IsWebhook}, userName: {user.Username}" );
            var clone = (Context.Channel as ITextChannel)?.CloneChannelAsync();
            if (clone != null)
            {
                await clone;
            }
        }
    }
}
