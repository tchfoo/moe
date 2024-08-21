using Discord;
using Discord.WebSocket;
using Moe.Models;
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
    var user = (SocketGuildUser)cmd.User;
    var guild = user.Guild;
    var subcommand = cmd.GetSubcommand();
    if (!service.IsAuthorized(user, out var error))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} " + error);
      return;
    }

    var handle = subcommand.Name switch
    {
      "list" => ListPatterns(cmd, guild),
      _ => throw new InvalidOperationException($"{Emotes.ErrorEmote} Unknown subcommand {subcommand.Name}")
    };

    await handle;
  }

  private async Task ListPatterns(SocketSlashCommand cmd, SocketGuild guild)
  {
    var patterns = await service.GetPatternsFromCache(guild.Id);
    if (patterns.Count == 0)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} There are no embed fixer patterns");
      return;
    }

    var embed = new PaginatableEmbedBuilder<EmbedFixerPattern>
      (5, patterns, items =>
        new EmbedBuilder()
          .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
          .WithTitle("Embed fixer patterns")
          .WithFields(items.Select(x => new EmbedFieldBuilder()
            .WithName(x.Pattern)
            .WithValue(x.Replacement)))
          .WithColor(Colors.Blurple)
      );

    await cmd.RespondAsync(embed: embed.Embed, components: embed.Components);
  }
}
