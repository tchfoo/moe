using Discord.WebSocket;

namespace TNTBot.Commands
{
  public class PingCommand : SlashCommandBase
  {
    public PingCommand() : base("ping", "Test slash command") { }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      await cmd.RespondAsync("Pong!");
    }
  }
}
