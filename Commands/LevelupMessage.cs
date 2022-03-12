using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
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
      var user = (SocketGuildUser)cmd.User;
      await service.ToggleLevelupMessage(user);
      var enabled = await service.IsLevelupMessageEnabled(user);

      var enabledOut = enabled ? "enabled" : "disabled";
      await cmd.RespondAsync($"{Emotes.SuccessEmote} Levelup message is now {enabledOut} for {user.Mention}");
    }
  }
}
