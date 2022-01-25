
using Discord;
using Discord.WebSocket;

namespace TNTBot
{
  public abstract class SlashCommandBase
  {
    private SocketGuild Guild { get => Services.Client.GetGuild(Config.Load().ServerID); }
    public abstract string CommandName { get; }
    public abstract Task Register();
    public abstract Task Handle(SocketSlashCommand cmd);
    protected async Task RegisterSlashCommand(SlashCommandBuilder builder)
    {
      await Guild.CreateApplicationCommandAsync(builder.Build());
    }
  }
}
