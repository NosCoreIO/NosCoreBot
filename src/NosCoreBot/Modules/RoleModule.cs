using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace NosCoreBot.Modules
{
    public class RoleModule : ModuleBase<SocketCommandContext>
    {
        private const string ContributorRoleName = "Contributor";
        private const string AnnouncementChannelName = "general";

        [Command("contributor")]
        [Name("contributor <user> <message?>")]
        [Summary("Give the Contributor role and announce it in #general")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Contributor(SocketGuildUser user, [Remainder] string message = null)
        {
            var role = Context.Guild.Roles.FirstOrDefault(r => r.Name == ContributorRoleName);
            if (role == null)
            {
                await ReplyAsync($"The {ContributorRoleName} role doesn't exist.");
                return;
            }

            if (user.Roles.Any(r => r.Id == role.Id))
            {
                await ReplyAsync($"{user.DisplayName} is already a {ContributorRoleName}.");
                return;
            }

            await user.AddRoleAsync(role);

            var channel = Context.Guild.TextChannels.FirstOrDefault(c => c.Name == AnnouncementChannelName)
                ?? (ISocketMessageChannel)Context.Channel;
            var thanks = string.IsNullOrWhiteSpace(message)
                ? "Thank you for the pull requests merged into NosCore!"
                : message;
            await channel.SendMessageAsync($"🎉 {user.Mention} is now a **{ContributorRoleName}**! {thanks}");

            if (channel.Id != Context.Channel.Id)
            {
                await ReplyAsync($"{user.DisplayName} got the {ContributorRoleName} role.");
            }
        }

        [Command("role-add")]
        [Name("role-add <user> <role>")]
        [Summary("Give a role to a user")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task AddRole(SocketGuildUser user, [Remainder] SocketRole role)
        {
            await user.AddRoleAsync(role);
            await ReplyAsync($"{user.DisplayName} got the role {role.Name}.");
        }

        [Command("role-remove")]
        [Name("role-remove <user> <role>")]
        [Summary("Take a role away from a user")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task RemoveRole(SocketGuildUser user, [Remainder] SocketRole role)
        {
            await user.RemoveRoleAsync(role);
            await ReplyAsync($"{user.DisplayName} lost the role {role.Name}.");
        }
    }
}
