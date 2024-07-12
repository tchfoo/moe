using Discord.WebSocket;
using Moe.Services;

namespace Moe.Commands;

public class LevelupMessage : SlashCommandBase
{
  private readonly LevelService service;

  public LevelupMessage(LevelService service) : base("levelup")
  {
    Description = "Toggle getting the levelup message";
    this.service = service;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    await cmd.DeferAsync();

    var user = (SocketGuildUser)cmd.User;
    await service.ToggleLevelupMessage(user);
    var enabled = await service.IsLevelupMessageEnabled(user);

    var enabledOut = enabled ? "enabled" : "disabled";
    await cmd.FollowupAsync($"{Emotes.SuccessEmote} Levelup message is now {enabledOut} for {user.Mention}");
  }
}
