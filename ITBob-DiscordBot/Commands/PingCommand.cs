using ITBob_DiscordBot.Resources;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace ITBob_DiscordBot.Commands;

public class PingCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ping", "Pong!")]
    public async Task<InteractionMessageProperties> Execute()
    {
        return new InteractionMessageProperties().WithContent(
                string.Format(Resource.Commands_Ping_Message, Context?.User.Username,
                    Context?.Client.Latency.Milliseconds))
            .WithFlags(MessageFlags.Ephemeral);
    }
}