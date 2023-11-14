using Discord;
using Discord.WebSocket;
using Moe.Models;
using Moe.Services;

namespace Moe.Commands;

public class AnnounceCommand : SlashCommandBase
{
  private readonly TemplateService service;

  public AnnounceCommand(TemplateService service) : base("announce")
  {
    Description = "Announce a template";
    Options = new SlashCommandOptionBuilder()
      .AddOption("name", ApplicationCommandOptionType.String, "The name of the template", isRequired: true)
      .AddOption("preview", ApplicationCommandOptionType.Boolean, "Whether to preview the template or actually announce it, default = false", isRequired: false);
    this.service = service;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    var user = (SocketGuildUser)cmd.User;
    var guild = user.Guild;
    var name = cmd.GetOption<string>("name")!;
    var preview = cmd.GetOption<bool>("preview");

    if (!await service.HasTemplate(guild, name))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} No template named {name}");
      return;
    }
    var template = (await service.GetTemplate(guild, name))!;

    if (!Authorize(user, template))
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} You are not the creator of the template {name}");
      return;
    }

    var paramNames = service.GetTemplateParameters(template);
    if (paramNames.Count == 0)
    {
      await ShowTemplate(cmd, template, preview);
    }
    else
    {
      var modal = CreateAnnounceModal(template, paramNames);
      modal.OnSubmitted += async submitted =>
      {
        var @params = paramNames.ToDictionary(x => x, x => submitted.GetValue(x) ?? "*unspecified*");

        template.Title = service.ReplaceTemplateParameters(template.Title, @params)!;
        template.Description = service.ReplaceTemplateParameters(template.Description, @params)!;
        template.Footer = service.ReplaceTemplateParameters(template.Footer, @params);

        await ShowTemplate(submitted, template, preview);
      };

      await cmd.RespondWithModalAsync(modal.Build());
    }
  }
  private bool Authorize(SocketGuildUser user, TemplateModel template)
  {
    if (template.Creator?.Id == user.Id)
    {
      return true;
    }

    return service.IsAuthorized(user, ModrankLevel.Owner, out _);
  }

  private SubmittableModalBuilder CreateAnnounceModal(TemplateModel template, List<string> @params)
  {
    var modal = new SubmittableModalBuilder()
      .WithTitle($"Announce: {template.Name}");

    foreach (var param in @params)
    {
      modal.AddTextInput(param, param, placeholder: TrimPlaceholder(param), required: false);
    }

    return (SubmittableModalBuilder)modal;
  }

  private string? TrimPlaceholder(string? placeholder)
  {
    if (placeholder?.Length > TextInputBuilder.MaxPlaceholderLength)
    {
      return placeholder.Substring(0, Math.Min(placeholder.Length, 96)) + "...";
    }

    return placeholder;
  }

  private async Task ShowTemplate(SocketInteraction interaction, TemplateModel template, bool preview)
  {
    if (preview)
    {
      await PreviewTemplate(interaction, template);
    }
    else
    {
      if (template.Channel is null)
      {
        await interaction.RespondAsync($"{Emotes.ErrorEmote} The channel for this template got deleted");
        return;
      }

      await AnnounceTemplate(interaction, template);
    }
  }

  private async Task AnnounceTemplate(SocketInteraction interaction, TemplateModel template)
  {
    var embed = GetAnnouncementEmbed(template);
    if (embed.Length > EmbedBuilder.MaxEmbedLength)
    {
      await interaction.RespondAsync($"{Emotes.ErrorEmote} You have reached the maximum embed character limit ({EmbedBuilder.MaxEmbedLength} characters), so the announcement cannot be displayed, try recreating the template but shorter");
      return;
    }

    var mention = template.Mention?.Mention;
    await template.Channel.SendMessageAsync(text: mention, embed: embed.Build());
    await interaction.RespondAsync($"{Emotes.SuccessEmote} Announced template **{template.Name}**");
  }

  private async Task PreviewTemplate(SocketInteraction interaction, TemplateModel template)
  {
    var embed = GetAnnouncementEmbed(template);
    if (embed.Length > EmbedBuilder.MaxEmbedLength)
    {
      await interaction.RespondAsync($"{Emotes.ErrorEmote} You have reached the maximum embed character limit ({EmbedBuilder.MaxEmbedLength} characters), so the announcement cannot be displayed, try recreating the template but shorter");
      return;
    }

    var mention = template.Mention?.Mention;
    await interaction.RespondAsync(text: mention, embed: embed.Build(), ephemeral: true);
  }

  private EmbedBuilder GetAnnouncementEmbed(TemplateModel template)
  {
    return new EmbedBuilder()
      .WithTitle(template.Title)
      .WithDescription(template.Description)
      .WithFooter(template.Footer)
      .WithThumbnailUrl(template.ThumbnailImageUrl)
      .WithImageUrl(template.LargeImageUrl)
      .WithColor(Colors.Blurple);
  }
}
