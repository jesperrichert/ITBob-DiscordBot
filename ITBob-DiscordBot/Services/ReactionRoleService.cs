using ITBob_DiscordBot.Database;
using ITBob_DiscordBot.Database.Entitys;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace ITBob_DiscordBot.Services;

public class ReactionRoleService
{
    private readonly DatabaseContext DatabaseContext;
    private readonly ConfigService ConfigService;

    public ReactionRoleService(DatabaseContext databaseContext, ConfigService configService)
    {
        DatabaseContext = databaseContext;
        ConfigService = configService;
    }

    public async Task<bool> IsReactionRoleGameChannelMessageAsync(ulong forumThreadId)
    {
        var reactionRole =
            await DatabaseContext.ReactionRoles.FirstOrDefaultAsync(r => r.ForumThreadId == forumThreadId);

        if (reactionRole == null)
        {
            return false;
        }

        return reactionRole.IsApproved && reactionRole.RoleId != 0;
    }

    public async Task<ReactionRolesEntity?> GetReactionRoleByMessageIdAsync(ulong messageId)
    {
        var reactionRole = DatabaseContext.ReactionRoles.FirstOrDefault(r => r.ReactionMessageId == messageId);
        if (reactionRole == null)
        {
            return null;
        }

        return reactionRole.IsApproved ? reactionRole : null;
    }

    public async Task<ReactionRolesEntity?> GetReactionRoleByForumThreadIdIdAsync(ulong forumThreadId)
    {
        var reactionRole = DatabaseContext.ReactionRoles.FirstOrDefault(r => r.ForumThreadId == forumThreadId);
        if (reactionRole == null)
        {
            return null;
        }

        return reactionRole.IsApproved ? reactionRole : null;
    }

    public async Task CreateReactionRoleAsync(string gameName, ulong guildId, ulong reactionMessageId,
        ulong reactionChannelId, ulong creatorId, ulong adminMessageId)
    {
        var reactionRole = new ReactionRolesEntity()
        {
            GuildId = guildId,
            RoleId = 0,
            GameName = gameName,
            ReactionMessageId = reactionMessageId,
            ReactionChannelId = reactionChannelId,
            ForumThreadId = 0,
            IsApproved = false,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow,
            AdminMessageId = adminMessageId
        };

        DatabaseContext.ReactionRoles.Add(reactionRole);
        await DatabaseContext.SaveChangesAsync();
    }

    public async Task<bool> ApproveReactionRoleAsync(ulong messageId, RestGuild guild)
    {
        var reactionRole = DatabaseContext.ReactionRoles.FirstOrDefault(r => r.ReactionMessageId == messageId);
        if (reactionRole == null)
        {
            return false;
        }

        if (reactionRole.IsApproved)
        {
            return false; // Already approved
        }

        var guildChannels = await guild.GetChannelsAsync();
        var reactionChannel =
            guildChannels.FirstOrDefault(c => c.Id == reactionRole.ReactionChannelId) as TextGuildChannel;
        if (reactionChannel == null)
        {
            return false;
        }

        var message = await reactionChannel.GetMessageAsync(reactionRole.ReactionMessageId);

        await message.AddReactionAsync(ConfigService.Get().FeatureConfig.ReactionRoles.ReactionEmoji);

        var role = await guild.CreateRoleAsync(
            new RoleProperties
            {
                Name = reactionRole.GameName,
                Colors = new RoleColorsProperties(new Color(46, 204, 113)),
                Mentionable = true,
                Permissions = Permissions.SendMessages,
            });

        var forumChannel =
            guildChannels.FirstOrDefault(c => c.Id == ConfigService.Get().FeatureConfig.ReactionRoles.ForumChannelId) as
                ForumGuildChannel;

        if (forumChannel == null)
        {
            return false;
        }

        var thread = await forumChannel.CreateForumGuildThreadAsync(
            new ForumGuildThreadProperties($"{reactionRole.GameName}",
                new ForumGuildThreadMessageProperties()
                {
                    Components =
                    [
                        new ComponentContainerProperties
                        {
                            new TextDisplayProperties(ConfigService.Get().Messages.ReactionRoleMessages
                                .ForumThreadCreated.Replace(
                                    "{0}", reactionRole.GameName).Replace("{1}",
                                    ConfigService.Get().FeatureConfig.ReactionRoles.RoleCreationChannelId.ToString()))
                        }
                    ],
                    Flags = MessageFlags.IsComponentsV2
                }
            ));

        reactionRole.RoleId = role.Id;
        reactionRole.ForumThreadId = thread.Id;
        reactionRole.IsApproved = true;

        DatabaseContext.ReactionRoles.Update(reactionRole);
        await DatabaseContext.SaveChangesAsync();

        return true;
    }


    public async Task<DeleteCallback?> DeleteReactionRoleByMessageIdAsync(ulong messageId)
    {
        var reactionRole = DatabaseContext.ReactionRoles.FirstOrDefault(r => r.ReactionMessageId == messageId);
        if (reactionRole == null)
        {
            return null;
        }

        DatabaseContext.ReactionRoles.Remove(reactionRole);
        await DatabaseContext.SaveChangesAsync();

        return new DeleteCallback
        {
            ReactionChannelId = reactionRole.ReactionChannelId,
            AdminMessageId = reactionRole.AdminMessageId,
            ReactionMessageId = reactionRole.ReactionMessageId
        };
    }

    public class DeleteCallback
    {
        public ulong AdminMessageId { get; set; }
        public ulong ReactionChannelId { get; set; }
        public ulong ReactionMessageId { get; set; }
    }
}