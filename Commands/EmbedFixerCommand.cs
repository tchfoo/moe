using Discord;
using Discord.WebSocket;
using Moe.Services;

namespace Moe.Commands;

public class EmbedFixerCommand : SlashCommandBase
{
  private readonly EmbedFixerService service;

  public EmbedFixerCommand(EmbedFixerService service) : base("embedfixer")
  {
    Options = new SlashCommandOptionBuilder()
      .AddOption(new SlashCommandOptionBuilder()
        .WithName("add")
        .WithDescription("Add a regex pattern and a replacement")
        .WithType(ApplicationCommandOptionType.SubCommand)
      ).AddOption(new SlashCommandOptionBuilder()
        .WithName("remove")
        .WithDescription("Remove a regex pattern and a replacement")
        .WithType(ApplicationCommandOptionType.SubCommand)
      ).AddOption(new SlashCommandOptionBuilder()
        .WithName("list")
        .WithDescription("List all regex patterns and replacements")
        .WithType(ApplicationCommandOptionType.SubCommand)
      ).WithType(ApplicationCommandOptionType.SubCommandGroup);
      ;
    this.service = service;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
  }
}
