using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Discord.WebSocket;
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
            var clone = (Context.Channel as ITextChannel)?.CloneChannelAsync();
            if (clone != null)
            {
                await clone;
            }
        }
    }
}
