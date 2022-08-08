using Discord.WebSocket;

namespace MoeBot.Commands;

public class PingCommand : SlashCommandBase
{
  public PingCommand() : base("ping")
  {
    Description = "Test slash command";
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    await cmd.RespondAsync("Pong!");
  }
}
