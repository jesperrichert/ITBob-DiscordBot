using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace ITBob_DiscordBot.Services;

public class VerifyService
{
    private readonly ILogger<VerifyService> Logger;
    private readonly ConfigService ConfigService;

    public VerifyService(ConfigService configService, ILogger<VerifyService> logger)
    {
        Logger = logger;
        ConfigService = configService;
    }

    public async Task SendVerifyLogMessageAsync(TextChannel channel, Role role, ulong userId, ulong executer)
    {
        await channel.SendMessageAsync(
            new MessageProperties
            {
                Components =
                [
                    new ComponentContainerProperties
                    {
                        new TextDisplayProperties(
                            "Role <@&{0}> has been added for user <@{2}> by <@{3}>."
                                .Replace("{0}", role.Id.ToString())
                                .Replace("{2}", userId.ToString()).Replace(
                                    "{3}", executer.ToString())),
                    }
                ],
                Flags = MessageFlags.IsComponentsV2
            }
        );
    }

    public async Task CreateVerifyRequestAsync(ulong userId, string name, string className, RestGuild guild)
    {
        var config = ConfigService.Get();

        var guildChannel =
            (await guild.GetChannelsAsync()).FirstOrDefault(c =>
                c.Id == config.FeatureConfig.Verify.AdminVerifyChannelId);

        if (guildChannel is null)
        {
            Logger.LogWarning("Verify channel not found in guild {GuildId}", guild.Id);
            return;
        }

        if (guildChannel is not TextChannel textChannel)
        {
            Logger.LogWarning("Verify channel is not a text channel in guild {GuildId}", guild.Id);
            return;
        }

        await textChannel.SendMessageAsync(
            new MessageProperties
            {
                Flags = MessageFlags.IsComponentsV2,
                Components =
                [
                    new ComponentContainerProperties
                    {
                        new TextDisplayProperties(
                            config.Messages.VerifyMessages.VerifyAdminRequestMessage
                                .Replace("{2}", userId.ToString())
                                .Replace("{0}", name)
                                .Replace("{1}", className)
                        ),
                        new ActionRowProperties
                        {
                            new ButtonProperties("verify-approve:" + userId + ":" + name + ":" + className,
                                EmojiProperties.Custom(1377021742072987719), ButtonStyle.Secondary)
                            {
                                Id = 2,
                            }
                        }
                    }
                ]
            });
    }
}