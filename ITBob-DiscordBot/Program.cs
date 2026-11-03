using ITBob_DiscordBot.Configuration;
using ITBob_DiscordBot.Database;
using ITBob_DiscordBot.Features.ReactionRoles.Handler;
using ITBob_DiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace ITBob_DiscordBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddDbContext<DatabaseContext>();

        // Load configuration
        var configService = new ConfigService();
        await configService.CreateConfig();
        var config = configService.Get();

        builder.Services.AddSingleton(configService);
        builder.Services.AddScoped<ReactionRoleService>();
        builder.Services.AddScoped<ReactionRoleChatInGameChannelHandler>();
        builder.Services.AddScoped<ReactionRoleCreationHandler>();
        builder.Services.AddScoped<VerifyService>();
        builder.Services.AddScoped<TempVoiceService>();

        builder.Services
            .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
            .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>()
            .AddComponentInteractions<UserMenuInteraction, UserMenuInteractionContext>()
            .AddComponentInteractions<RoleMenuInteraction, RoleMenuInteractionContext>()
            .AddComponentInteractions<MentionableMenuInteraction, MentionableMenuInteractionContext>()
            .AddComponentInteractions<ChannelMenuInteraction, ChannelMenuInteractionContext>()
            .AddComponentInteractions<ModalInteraction, ModalInteractionContext>()
            .AddApplicationCommands()
            .AddDiscordGateway(options =>
                {
                    options.Token = config.DiscordBot.Token;
                    options.Presence = new PresenceProperties(config.BotPresence.Status)
                    {
                        Activities =
                        [
                            new UserActivityProperties("", config.BotPresence.Type)
                                { State = config.BotPresence.Name }
                        ],
                        StatusType = config.BotPresence.Status,
                        Afk = false
                    };
                    options.Intents = GatewayIntents.All
                                      | GatewayIntents.DirectMessages
                                      | GatewayIntents.MessageContent
                                      | GatewayIntents.DirectMessageReactions
                                      | GatewayIntents.GuildMessageReactions
                                      | GatewayIntents.Guilds
                                      | GatewayIntents.GuildVoiceStates;
                }
            ).AddGatewayHandlers(typeof(Program).Assembly);

        var host = builder.Build();

        // Perform Database Migrations
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await db.Database.MigrateAsync();
        }

        host.AddModules(typeof(Program).Assembly);

        await host.RunAsync();
    }
}