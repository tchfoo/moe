using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class AnnounceCommand : SlashCommandBase
  {
    private readonly TemplateService service;
    private readonly Dictionary<string, TemplateModel> pendingModals;

    public AnnounceCommand(TemplateService service) : base("announce")
    {
      Description = "Announce a template";
      Options = new SlashCommandOptionBuilder()
        .AddOption("name", ApplicationCommandOptionType.String, "The name of the command to add", isRequired: true)
        .AddOption("preview", ApplicationCommandOptionType.Boolean, "Whether to preview the template or actually announce it, default = false", isRequired: false);
      this.service = service;
      pendingModals = new Dictionary<string, TemplateModel>();
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

      if (template.Creator.Equals(user))
      {
        await cmd.RespondAsync($"You are not the creator of the template {name}");
        return;
      }

      if (preview)
      {
        await PreviewAnnouncement(cmd, template);
      }
      else
      {
        var @params = service.GetTemplateParameters(template);
        if (@params.Count == 0)
        {
          await AnnounceTemplate(template);
          await cmd.RespondAsync($"Announced template **{template.Name}**");
        }
        else
        {
          var modal = BuildAnnounceModal(template, @params);
          await cmd.RespondWithModalAsync(modal);
        }
      }
    }

    private Modal BuildAnnounceModal(TemplateModel template, List<string> @params)
    {
      var modalId = Guid.NewGuid().ToString();
      pendingModals.Add(modalId, template);

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
      if (!pendingModals.TryGetValue(id, out var template))
      {
        return;
      }

      var paramNames = service.GetTemplateParameters(template);
      var @params = new Dictionary<string, string>();
      foreach (var paramName in paramNames)
      {
        @params.Add(paramName, modal.GetValue(paramName) ?? "*unspecified*");
      }
      template.Title = service.ReplaceTemplateParameters(template.Title, @params)!;
      template.Description = service.ReplaceTemplateParameters(template.Description, @params)!;
      template.Footer = service.ReplaceTemplateParameters(template.Footer, @params);

      await AnnounceTemplate(template);

      await modal.RespondAsync($"Announced template **{template.Name}**");
      pendingModals.Remove(id);
    }

    private Embed BuildAnnouncmentEmbed(TemplateModel template)
    {
      return new EmbedBuilder()
        .WithTitle(template.Title)
        .WithDescription(template.Description)
        .WithFooter(template.Footer)
        .WithThumbnailUrl(template.ThumbnailImageUrl)
        .WithImageUrl(template.LargeImageUrl)
        .Build();
    }

    private async Task AnnounceTemplate(TemplateModel template)
    {
      var embed = BuildAnnouncmentEmbed(template);
      var mention = template.Mention?.Mention;
      await template.Channel.SendMessageAsync(text: mention, embed: embed);
    }

    private async Task PreviewAnnouncement(SocketSlashCommand cmd, TemplateModel template)
    {
      var embed = BuildAnnouncmentEmbed(template);
      var mention = template.Mention?.Mention;
      await cmd.RespondAsync(text: mention, embed: embed, ephemeral: true);
    }
  }
}
