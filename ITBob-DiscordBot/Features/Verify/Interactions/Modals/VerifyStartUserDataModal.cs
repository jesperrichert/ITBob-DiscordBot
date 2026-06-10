using ITBob_DiscordBot.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace ITBob_DiscordBot.Features.Verify.Interactions.Modals;

public class VerifyStartUserDataModal : ComponentInteractionModule<ModalInteractionContext>
{
    private readonly ConfigService ConfigService;
    private readonly VerifyService VerifyService;

    public VerifyStartUserDataModal(ConfigService configService, VerifyService verifyService)
    {
        VerifyService = verifyService;
        ConfigService = configService;
    }

    [ComponentInteraction("verify-start-userdata-modal")]
    public async Task<InteractionMessageProperties> Modal()
    {
        var name = Context.Components.OfType<Label>()
            .Select(l => l.Component)
            .OfType<TextInput>()
            .FirstOrDefault(input => input.CustomId == "name");
        var classOption = Context.Components.OfType<Label>()
            .Select(l => l.Component)
            .OfType<TextInput>()
            .FirstOrDefault(input => input.CustomId == "class");
        var guild = await Context.Client.Rest.GetGuildAsync((ulong)Context.Interaction.GuildId);

        await VerifyService.CreateVerifyRequestAsync(
            Context.User.Id,
            name?.Value ?? "Unknown",
            classOption?.Value ?? "Unknown",
            guild
        );

        return new InteractionMessageProperties
        {
            Content = ConfigService.Get().Messages.VerifyMessages.VerifySuccessMessage,
            Flags = MessageFlags.Ephemeral
        };
    }
}