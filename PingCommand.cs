using Discord;
using Discord.WebSocket;

namespace TNTBot
{
  public class PingCommand : SlashCommandBase
  {
    public override string CommandName { get => "ping"; }
    public override async Task Register()
    {
      await RegisterSlashCommand(new SlashCommandBuilder()
        .WithName("ping")
        .WithDescription("Test slash command"));
    }
    public override async Task Handle(SocketSlashCommand cmd)
    {
      await cmd.RespondAsync("Pong!");
    }
  }
}
