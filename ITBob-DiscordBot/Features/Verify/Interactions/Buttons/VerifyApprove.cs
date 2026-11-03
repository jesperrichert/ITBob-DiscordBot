using ITBob_DiscordBot.Resources;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace ITBob_DiscordBot.Features.Verify.Interactions.Buttons;

public class VerifyApprove : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("verify-approve")]
    public async Task<InteractionMessageProperties> Button(ulong userId, string name, string className)
    {
        return new InteractionMessageProperties
        {
            Components =
            [
                new ActionRowProperties
                {
                    new ButtonProperties(
                        "verify-approve-add-fachkraft:" + userId + ":" + name + ":" + className + ":" +
                        Context.Interaction.Message.Id,
                        Resource.Features_Verify_Interaction_Buttons_Fachkraft_Approve_Label,
                        ButtonStyle.Success)
                    {
                        Emoji = EmojiProperties.Custom(1335667919119585480)
                    },
                    new ButtonProperties(
                        "verify-approve-add-layer8:" + userId + ":" + name + ":" + className + ":" +
                        Context.Interaction.Message.Id,
                        Resource.Features_Verify_Interaction_Buttons_Layer8_Approve_Label,
                        ButtonStyle.Success)
                    {
                        Emoji = EmojiProperties.Custom(1335667919119585480)
                    },

                    new ButtonProperties(
                        "verify-deny:" + Context.Message.Id,
                        Resource.Features_Verify_Interaction_Buttons_Deny,
                        ButtonStyle.Secondary)
                    {
                        Emoji = EmojiProperties.Custom(1322169218682322955)
                    }
                },
            ],
            Flags = MessageFlags.IsComponentsV2 | MessageFlags.Ephemeral,
        };
    }
}