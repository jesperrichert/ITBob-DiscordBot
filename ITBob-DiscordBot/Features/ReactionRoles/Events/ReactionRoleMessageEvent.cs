using ITBob_DiscordBot.Enums;
using ITBob_DiscordBot.Features.ReactionRoles.Handler;
using ITBob_DiscordBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace ITBob_DiscordBot.Features.ReactionRoles.Events;

public class ReactionRoleMessageEvent : IMessageCreateGatewayHandler
{
    private readonly ILogger<ReactionRoleMessageEvent> Logger;
    private readonly ConfigService CongigService;
    private readonly IServiceScopeFactory ServiceScopeFactory;

    public ReactionRoleMessageEvent(ILogger<ReactionRoleMessageEvent> logger, ConfigService configService,
        IServiceScopeFactory scopeFactory)
    {
        Logger = logger;
        CongigService = configService;
        ServiceScopeFactory = scopeFactory;
    }

    public async ValueTask HandleAsync(Message message)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var reactionRoleService = scope.ServiceProvider.GetRequiredService<ReactionRoleService>();
        var restClient = scope.ServiceProvider.GetRequiredService<RestClient>();
        var reactionRoleCreationHandler =
            scope.ServiceProvider.GetRequiredService<ReactionRoleCreationHandler>();
        var reactionRoleChatInGameChannelHandler =
            scope.ServiceProvider.GetRequiredService<ReactionRoleChatInGameChannelHandler>();
        var config = CongigService.Get();

        var reactionRoleMessageCreateType = ReactionRoleMessageCreateType.Unknown;

        if (message.Author.IsBot) return;
        if (message.ChannelId == config.FeatureConfig.ReactionRoles.RoleCreationChannelId)
            reactionRoleMessageCreateType = ReactionRoleMessageCreateType.IsReactionRoleCreate;
        if (await reactionRoleService.IsReactionRoleGameChannelMessageAsync(message.ChannelId))
            reactionRoleMessageCreateType = ReactionRoleMessageCreateType.IsPostMessageInGameForumThread;

        switch (reactionRoleMessageCreateType)
        {
            case ReactionRoleMessageCreateType.IsReactionRoleCreate:
            {
                await reactionRoleCreationHandler.Handle(
                    message, config, Logger, restClient, reactionRoleService
                );
            }
                break;
            case ReactionRoleMessageCreateType.IsPostMessageInGameForumThread:
            {
                var guild = await restClient.GetGuildAsync((ulong)message.GuildId!);
                var member = await guild.GetUserAsync(message.Author.Id);
                await reactionRoleChatInGameChannelHandler.Handle(
                    message, member, config, restClient, Logger, reactionRoleService);
            }
                break;
            case ReactionRoleMessageCreateType.Unknown:
                Logger.LogWarning("Unknown ReactionRoleMessageCreateType for Message {MessageId} in Guild {GuildId}",
                    message.Id, message.GuildId);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}