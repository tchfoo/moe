
using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public abstract class SlashCommandBase
  {
    private SocketGuild Guild { get => DiscordService.Discord.GetGuild(Config.Load().ServerID); }
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public SlashCommandOptionBuilder? Options { get; protected set; }

    protected SlashCommandBase(string name, string description)
    {
      Name = name;
      Description = description;
    }

    public virtual Task OnRegister() => Task.CompletedTask;

    public abstract Task Handle(SocketSlashCommand cmd);

    public async Task Register()
    {
      var builder = new SlashCommandBuilder()
        .WithName(Name)
        .WithDescription(Description);
      if (Options != null)
      {
        builder.AddOptions(Options.Options.ToArray());
      }
      await Guild.CreateApplicationCommandAsync(builder.Build());
      await OnRegister();
    }
  }
}
