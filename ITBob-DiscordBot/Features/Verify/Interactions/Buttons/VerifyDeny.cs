using ITBob_DiscordBot.Resources;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace ITBob_DiscordBot.Features.Verify.Interactions.Buttons;

public class VerifyDeny : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("verify-deny")]
    public async Task<InteractionMessageProperties?> Button(ulong messageId)
    {
        var message = await Context.Interaction.Channel.GetMessageAsync(messageId);
        await message.DeleteAsync();
        return new InteractionMessageProperties
        {
            Content = Resource.Features_Verify_Interaction_Buttons_Deny_Text,
            Flags = MessageFlags.Ephemeral
        };
    }
}