using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class AnnounceCommand : SlashCommandBase
  {
    private readonly TemplateService service;

    public AnnounceCommand(TemplateService service) : base("announce")
    {
      Description = "Announce a template";
      Options = new SlashCommandOptionBuilder()
        .AddOption("name", ApplicationCommandOptionType.String, "The name of the command to add", isRequired: true)
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
        await cmd.RespondAsync($"No template named {name}");
        return;
      }
      var template = (await service.GetTemplate(guild, name))!;

      if (!Authorize(user, template))
      {
        await cmd.RespondAsync($"You are not the creator of the template {name}");
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
        await cmd.RespondWithModalAsync(modal.Build());

        var _ = HandleAnnounceModalSubmission(modal, paramNames, template, preview);
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
        modal.AddTextInput(param, param, placeholder: param, required: false);
      }

      return (SubmittableModalBuilder)modal;
    }

    private async Task HandleAnnounceModalSubmission(SubmittableModalBuilder modal, List<string> paramNames, TemplateModel template, bool preview)
    {
      var submitted = await modal.WaitForSubmission();
      var @params = paramNames.ToDictionary(x => x, x => submitted.GetValue(x) ?? "*unspecified*");
      SubstituteTemplateParameters(template, @params);

      await ShowTemplate(submitted, template, preview);
    }

    private void SubstituteTemplateParameters(TemplateModel template, Dictionary<string, string> @params)
    {
      template.Title = service.ReplaceTemplateParameters(template.Title, @params)!;
      template.Description = service.ReplaceTemplateParameters(template.Description, @params)!;
      template.Footer = service.ReplaceTemplateParameters(template.Footer, @params);
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
