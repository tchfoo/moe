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
    this.service = service;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    var user = (SocketGuildUser)cmd.User;
    var guild = user.Guild;
    var subcommand = cmd.GetSubcommand();
    if (!Authorize(user, subcommand.Name, out var error))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} " + error);
      return;
    }

    var handle = subcommand.Name switch
    {
      "add" => AddPattern(cmd, subcommand, guild),
      "remove" => RemovePattern(cmd, subcommand, guild),
      "list" => ListPatterns(cmd, guild),
      _ => throw new InvalidOperationException($"{Emotes.ErrorEmote} Unknown subcommand {subcommand.Name}")
    };

    await handle;
  }

  private bool Authorize(SocketGuildUser user, string subcommand, out string? error)
  {
    error = null;
    if (subcommand == "list")
    {
      return true;
    }

    return service.IsAuthorized(user, ModrankLevel.Administrator, out error);
  }

  private async Task AddPattern(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
  {
    var modal = (SubmittableModalBuilder)new SubmittableModalBuilder()
      .WithTitle("Add embed fixer pattern")
      .AddTextInput("Regular expression pattern", nameof(EmbedFixerPattern.Pattern), placeholder: @"https?://(?:www\.)?instagram.com", required: true)
      .AddTextInput("Replacement", nameof(EmbedFixerPattern.Replacement), placeholder: "https://ddinstagram.com", required: true);

    modal.OnSubmitted += async submitted =>
    {
      var pattern = new EmbedFixerPattern()
      {
        Pattern = submitted.GetValue(nameof(EmbedFixerPattern.Pattern))!,
        Replacement = submitted.GetValue(nameof(EmbedFixerPattern.Replacement))!
      };

      if (await service.HasPatternInCache(guild, pattern.Pattern))
      {
        await submitted.RespondAsync($"{Emotes.ErrorEmote} Pattern already exists\n> {pattern.Pattern}\n> {pattern.Replacement}");
        return;
      }

      await service.AddPattern(guild, pattern);
      await submitted.RespondAsync($"{Emotes.SuccessEmote} Added embed fixer pattern");
    };

    await cmd.RespondWithModalAsync(modal.Build());
  }

  private async Task RemovePattern(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
  {
    var patterns = await service.GetPatternsFromCache(guild);
    if (patterns.Count == 0)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} There are no embed fixer patterns");
      return;
    }

    var options = patterns.Select(x =>
        new SelectMenuOptionBuilder(x.Pattern, x.Pattern, x.Replacement)
      ).ToList();

    var menu = (SubmittableSelectMenuBuilder)new SubmittableSelectMenuBuilder()
      .WithPlaceholder("Choose embed fixer patterns to remove")
      .WithOptions(options)
      .WithMinValues(0)
      .WithMaxValues(options.Count());

    menu.OnSubmitted += async submitted =>
    {
      var patternsInDatabase = await service.GetPatternsFromCache(guild);
      var patternsToRemove = patternsInDatabase
        .Where(x => submitted.Data.Values.Contains(x.Pattern))
        .ToList();

      if (!patternsToRemove.Any())
      {
        return;
      }

      var removalTasks = patternsToRemove.Select(x => service.RemovePattern(guild, x));
      await Task.WhenAll(removalTasks);

      var removedPatternsMessage = string.Join('\n', patternsToRemove
        .Select(x => $"> **{x.Pattern}**\n> {x.Replacement}")
      );
      await submitted.RespondAsync($"{Emotes.SuccessEmote} Removed {patternsToRemove.Count} embed fixer patterns:\n{removedPatternsMessage}");
    };

    var components = new ComponentBuilder()
        .WithSelectMenu(menu);
    await cmd.RespondAsync(components: components.Build(), ephemeral: true);
  }

  private async Task ListPatterns(SocketSlashCommand cmd, SocketGuild guild)
  {
    var patterns = await service.GetPatternsFromCache(guild);
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
