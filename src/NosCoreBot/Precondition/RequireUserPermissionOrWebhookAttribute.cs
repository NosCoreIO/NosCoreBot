using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace NosCoreBot.Precondition
{
    public class RequireUserPermissionOrWebhookAttribute : RequireUserPermissionAttribute
    {
        private readonly string[] _webhookNames;

        public RequireUserPermissionOrWebhookAttribute(Discord.GuildPermission permission, string[] webhookNames) : base(permission)
        {
            _webhookNames = webhookNames;
        }

        public RequireUserPermissionOrWebhookAttribute(Discord.ChannelPermission permission, string[] webhookNames) : base(permission)
        {
            _webhookNames = webhookNames;
        }
    

        public override Task<PreconditionResult> CheckPermissionsAsync(
          ICommandContext context,
          CommandInfo command,
          IServiceProvider services)
        {
            IGuildUser user = context.User as IGuildUser;
            if (context.User.IsWebhook)
            {
                return Task.FromResult(_webhookNames.Contains(context.User.Username) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Command must be used by an authorized webhook."));
            }

            if (this.GuildPermission.HasValue)
            {
                if (user == null)
                    return Task.FromResult(PreconditionResult.FromError(this.NotAGuildErrorMessage ?? "Command must be used in a guild channel."));
                if (!user.GuildPermissions.Has(this.GuildPermission.Value))
                    return Task.FromResult(PreconditionResult.FromError(this.ErrorMessage ?? string.Format("User requires guild permission {0}.", (object)this.GuildPermission.Value)));
            }
            Discord.ChannelPermission? channelPermission = this.ChannelPermission;
            if (channelPermission.HasValue)
            {
                ChannelPermissions channelPermissions = !(context.Channel is IGuildChannel channel) ? ChannelPermissions.All((IChannel)context.Channel) : user.GetPermissions(channel);
                ref ChannelPermissions local = ref channelPermissions;
                channelPermission = this.ChannelPermission;
                long num = (long)channelPermission.Value;
                if (!local.Has((Discord.ChannelPermission)num))
                {
                    string reason = this.ErrorMessage;
                    if (reason == null)
                    {
                        channelPermission = this.ChannelPermission;
                        reason = string.Format("User requires channel permission {0}.", (object)channelPermission.Value);
                    }
                    return Task.FromResult(PreconditionResult.FromError(reason));
                }
            }
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
