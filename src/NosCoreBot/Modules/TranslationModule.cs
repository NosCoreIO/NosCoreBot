using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Discord.WebSocket;
using NosCoreBot.Enumerations;
using NosCoreBot.Extensions;

namespace NosCoreBot.Modules
{
    public class TranslationModule : ModuleBase<SocketCommandContext>
    {
        [Command("translation-message")]
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
