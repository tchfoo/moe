using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class AnnounceCommand : SlashCommandBase
  {
    private readonly TemplateService service;
    private readonly Dictionary<string, (TemplateModel template, bool preview)> pendingModals;

    public AnnounceCommand(TemplateService service) : base("announce")
    {
      Description = "Announce a template";
      Options = new SlashCommandOptionBuilder()
        .AddOption("name", ApplicationCommandOptionType.String, "The name of the command to add", isRequired: true)
        .AddOption("preview", ApplicationCommandOptionType.Boolean, "Whether to preview the template or actually announce it, default = false", isRequired: false);
      this.service = service;
      pendingModals = new Dictionary<string, (TemplateModel template, bool preview)>();
      DiscordService.Discord.ModalSubmitted += OnModalSubmitted;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = (SocketGuildUser)cmd.User;
      var guild = user.Guild;
      var name = cmd.GetOption<string>("name")!;
      var preview = cmd.GetOption<bool>("preview");

      if (!await service.HasTemplate(guild, name))
      {
        await cmd.RespondAsync($"No template named {name}");
        return;
      }
      var template = (await service.GetTemplate(guild, name))!;

      if (!Authorize(user, template))
      {
        await cmd.RespondAsync($"You are not the creator of the template {name}");
        return;
      }

      var @params = service.GetTemplateParameters(template);
      if (@params.Count == 0)
      {
        await ShowTemplate(cmd, template, preview);
      }
      else
      {
        var modal = BuildAnnounceModal(template, preview, @params);
        await cmd.RespondWithModalAsync(modal);
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

    private Modal BuildAnnounceModal(TemplateModel template, bool preview, List<string> @params)
    {
      var modalId = Guid.NewGuid().ToString();
      pendingModals.Add(modalId, (template, preview));

      var modal = new ModalBuilder()
        .WithTitle($"Announce: {template.Name}")
        .WithCustomId(modalId);

      foreach (var param in @params)
      {
        modal.AddTextInput(param, param, placeholder: param, required: false);
      }

      return modal.Build();
    }

    private async Task OnModalSubmitted(SocketModal modal)
    {
      var id = modal.Data.CustomId;
      if (!pendingModals.TryGetValue(id, out var data))
      {
        return;
      }

      var (template, preview) = data;
      var paramNames = service.GetTemplateParameters(template);
      var @params = paramNames.ToDictionary(x => x, x => modal.GetValue(x) ?? "*unspecified*");

      template.Title = service.ReplaceTemplateParameters(template.Title, @params)!;
      template.Description = service.ReplaceTemplateParameters(template.Description, @params)!;
      template.Footer = service.ReplaceTemplateParameters(template.Footer, @params);

      await ShowTemplate(modal, template, preview);

      pendingModals.Remove(id);
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
          await interaction.RespondAsync("The channel for this template got deleted");
          return;
        }

        await AnnounceTemplate(template);
        await interaction.RespondAsync($"Announced template **{template.Name}**");
      }
    }

    private async Task AnnounceTemplate(TemplateModel template)
    {
      var embed = BuildAnnouncmentEmbed(template);
      var mention = template.Mention?.Mention;
      await template.Channel.SendMessageAsync(text: mention, embed: embed);
    }

    private async Task PreviewTemplate(SocketInteraction interaction, TemplateModel template)
    {
      var embed = BuildAnnouncmentEmbed(template);
      var mention = template.Mention?.Mention;
      await interaction.RespondAsync(text: mention, embed: embed, ephemeral: true);
    }

    private Embed BuildAnnouncmentEmbed(TemplateModel template)
    {
      return new EmbedBuilder()
        .WithTitle(template.Title)
        .WithDescription(template.Description)
        .WithFooter(template.Footer)
        .WithThumbnailUrl(template.ThumbnailImageUrl)
        .WithImageUrl(template.LargeImageUrl)
        .WithColor(Colors.Blurple)
        .Build();
    }
  }
}
