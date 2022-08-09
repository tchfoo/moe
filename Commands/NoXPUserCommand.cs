using Discord.WebSocket;
using MoeBot.Models;
using MoeBot.Services;

namespace MoeBot.Commands;

public class NoXPUserCommand : UserCommandBase
{
  private readonly LevelService service;

  public NoXPUserCommand(LevelService service) : base("Toggle No-XP")
  {
    this.service = service;
  }

  public override async Task Handle(SocketUserCommand cmd)
  {
    var user = (SocketGuildUser)cmd.User;
    var guild = user.Guild;
    var member = (SocketGuildUser)cmd.Data.Member;

    if (!service.IsAuthorized(user, ModrankLevel.Moderator, out var error))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} {error}");
      return;
    }

    if (!await service.IsNoXPRoleSet(guild))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} No-XP role was not set");
      return;
    }

    var isApplied = await service.IsNoXPRoleApplied(member);

    await service.ToggleNoXPRole(member);
    isApplied = !isApplied;

    if (isApplied)
    {
      await cmd.RespondAsync($"{Emotes.SuccessEmote} No-XP role added to {member.Mention}");
    }
    else
    {
      await cmd.RespondAsync($"{Emotes.SuccessEmote} No-XP role removed from {member.Mention}");
    }
  }
}
