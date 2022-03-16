
using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public abstract class SlashCommandBase
  {
    private SocketGuild Guild { get => DiscordService.Discord.GetGuild(Config.Load().ServerID); }
    private string? description;

    public string Name { get; protected set; }
    public string? Description
    {
      get => description ?? "No description";
      protected set => description = value;
    }
    public SlashCommandOptionBuilder? Options { get; protected set; }

    protected SlashCommandBase(string name)
    {
      Name = name;
    }

    public virtual Task OnRegister() => Task.CompletedTask;

    public abstract Task Handle(SocketSlashCommand cmd);

    public async Task Register()
    {
      var builder = new SlashCommandBuilder()
        .WithName(Name)
        .WithDescription(Description);
      if (Options is not null)
      {
        builder.AddOptions(Options.Options.ToArray());
      }

      // await Guild.CreateApplicationCommandAsync(builder.Build());
      await DiscordService.Discord.CreateGlobalApplicationCommandAsync(builder.Build());
      await OnRegister();
    }
  }
}
