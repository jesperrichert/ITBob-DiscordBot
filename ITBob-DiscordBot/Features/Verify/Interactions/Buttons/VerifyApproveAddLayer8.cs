using ITBob_DiscordBot.Resources;
using ITBob_DiscordBot.Services;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace ITBob_DiscordBot.Features.Verify.Interactions.Buttons;

public class VerifyApproveAddLayer8 : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ILogger<VerifyApproveAddLayer8> Logger;
    private readonly ConfigService ConfigService;
    private readonly VerifyService VerifyService;

    public VerifyApproveAddLayer8(ILogger<VerifyApproveAddLayer8> logger, ConfigService configService,
        VerifyService verifyService)
    {
        VerifyService = verifyService;
        ConfigService = configService;
        Logger = logger;
    }

    [ComponentInteraction("verify-approve-add-layer8")]
    public async Task Button(ulong userId, string name, string className,
        ulong interactionMessageId)
    {
        if (className != "" && className != "Unknown")
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(
                new InteractionMessageProperties().WithContent(
                        Resource.Features_Verify_Interaction_Buttons_Layer8_Add_HasClassName)
                    .WithFlags(MessageFlags.Ephemeral)));
            return;
        }

        var guild = await Context.Client.Rest.GetGuildAsync((ulong)Context.Interaction.GuildId!);
        var member = await guild.GetUserAsync(userId);

        await member.ModifyAsync(options =>
            options.Nickname = $"{name}");

        var role = await guild.GetRoleAsync(ConfigService.Get().FeatureConfig.Verify.Layer8RoleId);

        if (member.RoleIds.Contains(role.Id))
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(
                new InteractionMessageProperties().WithContent(
                        Resource.Features_Verify_Interaction_Buttons_Layer8_Add_HasRole
                    )
                    .WithFlags(MessageFlags.Ephemeral)));
            return;
        }

        await member.AddRoleAsync(role.Id);

        await VerifyService.SendVerifyLogMessageAsync(
            (TextChannel)(await guild.GetChannelsAsync()).FirstOrDefault(channel => channel.Id == ConfigService
                .Get().FeatureConfig.Verify
                .AdminVerifyChannelId
            )!,
            role, userId, Context.Interaction.User.Id);

        var interactionMessage = await Context.Channel.GetMessageAsync(interactionMessageId);
        await interactionMessage.DeleteAsync();

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithContent(
                    Resource.Features_Verify_Interaction_Buttons_Layer8_Add_Success
                )
                .WithFlags(MessageFlags.Ephemeral)));
    }
}