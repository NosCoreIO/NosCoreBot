using System.Threading.Tasks;
using Discord;

namespace NosCoreBot.Extensions
{
    //https://github.com/Yucked/Krypts/blob/master/DiscordExtensions.cs
    public static class ChannelExtension
    {
        public static async Task<ITextChannel> CloneChannelAsync(this ITextChannel channel)
        {
            var guild = channel.Guild;
            var newChannel = await guild.CreateTextChannelAsync(channel.Name,
                x =>
                {
                    x.Topic = channel.Topic;
                    x.IsNsfw = channel.IsNsfw;
                    x.Position = channel.Position;
                    x.CategoryId = channel.CategoryId;
                });

            foreach (var permissionOverwrite in channel.PermissionOverwrites)
                switch (permissionOverwrite.TargetType)
                {
                    case PermissionTarget.Role:
                        var role = guild.GetRole(permissionOverwrite.TargetId);
                        await newChannel.AddPermissionOverwriteAsync(role, permissionOverwrite.Permissions);
                        break;

                    case PermissionTarget.User:
                        var user = await guild.GetUserAsync(permissionOverwrite.TargetId);
                        await newChannel.AddPermissionOverwriteAsync(user, permissionOverwrite.Permissions);
                        break;
                }

            var hooks = await channel.GetWebhooksAsync();
            foreach (var hook in hooks)
            {
                await hook.ModifyAsync(param => param.ChannelId = newChannel.Id);
            }

            await channel.DeleteAsync();
            return newChannel;
        }
    }
}
