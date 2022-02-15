
using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public abstract class MessageCommandBase
  {
    private SocketGuild Guild { get => DiscordService.Discord.GetGuild(Config.Load().ServerID); }

    public string Name { get; protected set; }

    protected MessageCommandBase(string name)
    {
      Name = name;
    }

    public virtual Task OnRegister() => Task.CompletedTask;

    public abstract Task Handle(SocketMessageCommand cmd);

    public async Task Register()
    {
      var builder = new SlashCommandBuilder().WithName(Name);
      await Guild.CreateApplicationCommandAsync(builder.Build());
      await OnRegister();
    }
  }
}
