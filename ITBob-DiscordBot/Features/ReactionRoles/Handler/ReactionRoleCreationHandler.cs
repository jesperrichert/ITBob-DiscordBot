using ITBob_DiscordBot.Configuration;
using ITBob_DiscordBot.Resources;
using ITBob_DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace ITBob_DiscordBot.Features.ReactionRoles.Handler;

public class ReactionRoleCreationHandler
{
    public async Task Handle(Message message, AppConfig config, ILogger logger, RestClient restClient,
        ReactionRoleService reactionRoleService)
    {
        if (message.Content.Length < 2)
        {
            await message.ReplyAsync(config.Messages.ReactionRoleMessages.Errors.ReactionRoleRoleNameToShort);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        var isExistRoleWithName = (await (await restClient.GetGuildAsync((ulong)message.GuildId)).GetRolesAsync()
            ).FirstOrDefault(role => role.Name.Equals(message.Content, StringComparison.OrdinalIgnoreCase));

        if (isExistRoleWithName != null)
        {
            var errorMessage = await message.ReplyAsync(
                config.Messages.ReactionRoleMessages.Errors.ReactionRoleRoleNameAlreadyExist.Replace("{0}",
                    isExistRoleWithName.Name));
            await Task.Delay(TimeSpan.FromSeconds(5));
            logger.LogWarning("Role with name {RoleName} already exists in guild {GuildId}", isExistRoleWithName.Name,
                message.GuildId);
            await message.DeleteAsync();
            await errorMessage.DeleteAsync();
            return;
        }

        await HandleConfirmMessage(message, config);

        // Admin Message
        var channels = await restClient.GetGuildChannelsAsync((ulong)message.GuildId);

        var adminChannel = channels.FirstOrDefault(channel =>
            channel.Id == config.FeatureConfig.ReactionRoles.AdminRoleApproveChannelId);

        switch (adminChannel)
        {
            case null:
                return;
            case TextGuildChannel textChannel:
                var adminMessage = await textChannel.SendMessageAsync(new MessageProperties()
                {
                    Components =
                    [
                        new ComponentContainerProperties()
                        {
                            new TextDisplayProperties(config.Messages.ReactionRoleMessages.AdminReactionRoleStartProcess
                                .Replace("{0}",
                                    message.Content).Replace("{1}", message.Author.Id.ToString())
                                .Replace("{2}", message.Content).Replace("{3}",
                                    $"https://discord.com/channels/{message.GuildId}/{message.ChannelId}/{message.Id}")),
                            new ActionRowProperties
                            {
                                new ButtonProperties("reactionrole-approve:" + message.Id + ":" + message.GuildId,
                                    Resource.Features_ReactionRoles_Handler_ReactionRole_Approve_Label,
                                    ButtonStyle.Success)
                                {
                                    Disabled = false,
                                    Emoji = EmojiProperties.Standard(":white_check_mark:"),
                                    Id = 1
                                },
                                new ButtonProperties("reactionrole-deny:" + message.Id,
                                    Resource.Features_ReactionRoles_Handler_ReactionRole_Deny_Label,
                                    ButtonStyle.Danger)
                                {
                                    Disabled = false,
                                    Emoji = EmojiProperties.Standard(":x:"),
                                    Id = 2
                                }
                            }
                        }
                    ],
                    Flags = MessageFlags.IsComponentsV2
                });

                await reactionRoleService.CreateReactionRoleAsync(message.Content, (ulong)message.GuildId, message.Id,
                    message.ChannelId, message.Author.Id, adminMessage.Id);

                break;
            default:
                logger.LogWarning("I cant send a Message in the Admin Channel {AdminChannelName}", adminChannel?.Name);
                break;
        }

        return;
    }

    private async Task HandleConfirmMessage(Message message, AppConfig config)
    {
        var msg = await message.ReplyAsync(config.FeatureConfig.ReactionRoles.UserInfoMessageAfterCreate);

        _ = Task.Run(async () =>
        {
            await Task.Delay(
                TimeSpan.FromSeconds(config.FeatureConfig.ReactionRoles.AutoDeleteUserInfoMessageAfterCreate));
            await msg?.DeleteAsync()!;
        });
    }
}