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
                    "You not can verify a member without a Class as Fachkraft.").WithFlags(MessageFlags.Ephemeral)));
            return;
        }
        var guild = await Context.Client.Rest.GetGuildAsync((ulong)Context.Interaction.GuildId);
        if (guild is null)
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties().WithContent("Guild not found. Please try again later.").WithFlags(MessageFlags.Ephemeral)));

        var member = await guild.GetUserAsync(userId);

        await member.ModifyAsync(options =>
            options.Nickname = $"{name} - {className}");

        var role = await guild.GetRoleAsync(ConfigService.Get().FeatureConfig.Verify.FachkraftRoleId);

        if (role == null)
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties().WithContent("Fachkraft role not found. Please contact an admin.").WithFlags(MessageFlags.Ephemeral)));

        if (member.RoleIds.Contains(role.Id))
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties()
                .WithContent("User already has the Fachkraft role.").WithFlags(MessageFlags.Ephemeral)));
            return;
        }
        await member.AddRoleAsync(role.Id);

        await VerifyService.SendVerifyLogMessageAsync(
            (TextChannel)(await guild.GetChannelsAsync()).FirstOrDefault(channel => channel.Id == ConfigService
                .Get().FeatureConfig.Verify
                .AdminVerifyChannelId
            ), role, userId, Context.Interaction.User.Id);

        var interactionMessage = await Context.Channel.GetMessageAsync(interactionMessageId);
        await interactionMessage.DeleteAsync();

        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties().WithContent("Successfully verified.").WithFlags(MessageFlags.Ephemeral)));
    }
}