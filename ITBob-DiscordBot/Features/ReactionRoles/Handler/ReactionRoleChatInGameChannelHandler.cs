using ITBob_DiscordBot.Configuration;
using ITBob_DiscordBot.Services;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace ITBob_DiscordBot.Features.ReactionRoles.Handler;

public class ReactionRoleChatInGameChannelHandler
{
    public async Task Handle(Message message, GuildUser member, AppConfig config, RestClient client, ILogger logger,
        ReactionRoleService reactionRoleService)
    {
        var reactionRole = await reactionRoleService.GetReactionRoleByForumThreadIdIdAsync(message.ChannelId);
        var isSendMessageFromNotGameUsersAllowed =
            config.FeatureConfig.ReactionRoles.AllowOtherToPostInRoleSpecifyChannel;
        if (member == null)
        {
            logger.LogWarning("ReactionRoleChatInGameChannelHandler: Message author is not a guild user");
            return;
        }

        if (message.GuildId is 0 or null)
        {
            logger.LogWarning("ReactionRoleChatInGameChannelHandler: Message is not from a guild");
            return;
        }

        var guild = await client.GetGuildAsync((ulong)message.GuildId);
        var guildChannels = await guild.GetActiveThreadsAsync();
        var forumThreadChannel =
            guildChannels.FirstOrDefault(guildChannel => guildChannel.Id == message.ChannelId);

        var hasMemberRoleInGameChannel = member.RoleIds.Contains(reactionRole.RoleId);

        if (isSendMessageFromNotGameUsersAllowed ||
            hasMemberRoleInGameChannel) return;

        var errorMessage =
            await forumThreadChannel?.SendMessageAsync(config.Messages.ReactionRoleMessages
                .IsNotAllowedToSendMessagesInGameThread)!;

        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            await message.DeleteAsync();
            await errorMessage.DeleteAsync();
        });
        return;
    }
}