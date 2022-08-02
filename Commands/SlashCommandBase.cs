
using Discord;
using Discord.WebSocket;

namespace TNTBot.Commands;

public abstract class SlashCommandBase
{
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

  public abstract Task Handle(SocketSlashCommand cmd);

  public SlashCommandProperties GetCommandProperties()
  {
    var builder = new SlashCommandBuilder()
      .WithName(Name)
      .WithDescription(Description);
    if (Options is not null)
    {
      builder.AddOptions(Options.Options.ToArray());
    }

    return builder.Build();
  }
}
