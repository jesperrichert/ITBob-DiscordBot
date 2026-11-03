using ITBob_DiscordBot.Resources;
using ITBob_DiscordBot.Services;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace ITBob_DiscordBot.Features.Verify.Interactions.Buttons;

public class VerifyApproveAddFachkraft : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ILogger<VerifyApproveAddFachkraft> Logger;
    private readonly ConfigService ConfigService;
    private readonly VerifyService VerifyService;

    public VerifyApproveAddFachkraft(ILogger<VerifyApproveAddFachkraft> logger, ConfigService configService,
        VerifyService verifyService)
    {
        Logger = logger;
        ConfigService = configService;
        VerifyService = verifyService;
    }

    [ComponentInteraction("verify-approve-add-fachkraft")]
    public async Task Button(ulong userId, string name, string className, ulong interactionMessageId)
    {
        if (className is "" or "Unknown")
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(
                new InteractionMessageProperties().WithContent(
                        Resource.Features_Verify_Interaction_Buttons_Fachkraft_Add_NoClassName)
                    .WithFlags(MessageFlags.Ephemeral)));
            return;
        }

        var guild = await Context.Client.Rest.GetGuildAsync((ulong)Context.Interaction.GuildId!);

        var member = await guild.GetUserAsync(userId);

        await member.ModifyAsync(options =>
            options.Nickname = $"{name} - {className}");

        var role = await guild.GetRoleAsync(ConfigService.Get().FeatureConfig.Verify.FachkraftRoleId);

        if (member.RoleIds.Contains(role.Id))
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties()
                .WithContent(
                    Resource.Features_Verify_Interaction_Buttons_Fachkraft_Add_HasRole)
                .WithFlags(MessageFlags.Ephemeral)));
            return;
        }

        await member.AddRoleAsync(role.Id);

        await VerifyService.SendVerifyLogMessageAsync(
            (TextChannel)(await guild.GetChannelsAsync()).FirstOrDefault(channel => channel.Id == ConfigService
                .Get().FeatureConfig.Verify
                .AdminVerifyChannelId
            )!, role, userId, Context.Interaction.User.Id);

        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties()
            .WithContent(
                Resource.Features_Verify_Interaction_Buttons_Fachkraft_Add_Success
            ).WithFlags(MessageFlags.Ephemeral)));

        var interactionMessage = await Context.Channel.GetMessageAsync(interactionMessageId);
        await interactionMessage.DeleteAsync();
    }
}