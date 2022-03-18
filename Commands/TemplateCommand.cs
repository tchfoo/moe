using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class TemplateCommand : SlashCommandBase
  {
    private readonly TemplateService service;

    public TemplateCommand(TemplateService service) : base("template")
    {
      Description = "Create, remove or list /announce templates";
      Options = new SlashCommandOptionBuilder()
        .AddOption(new SlashCommandOptionBuilder()
          .WithName("add")
          .WithDescription("Create a template")
          .AddOption("name", ApplicationCommandOptionType.String, "The name of the template", isRequired: true)
          .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel to announce the template in", isRequired: true)
          .AddOption("mention", ApplicationCommandOptionType.Role, "The role to mention when the template is sent", isRequired: false)
          .AddOption("hidden", ApplicationCommandOptionType.Boolean, "Whether to hide the template from the template list, default = false", isRequired: false)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("remove")
          .WithDescription("Remove a template")
          .AddOption("name", ApplicationCommandOptionType.String, "The name of the template to remove", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("dump")
          .WithDescription("Dump a template")
          .AddOption("name", ApplicationCommandOptionType.String, "The name of the template to dump", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("list")
          .WithDescription("List templates")
          .WithType(ApplicationCommandOptionType.SubCommand)
      ).WithType(ApplicationCommandOptionType.SubCommandGroup);
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = (SocketGuildUser)cmd.User;
      var subcommand = cmd.GetSubcommand();

      if (!Authorize(user, subcommand.Name, out var error))
      {
        await cmd.RespondAsync(error);
        return;
      }

      var handle = subcommand.Name switch
      {
        "add" => AddTemplate(cmd, subcommand, user),
        "remove" => RemoveTemplate(cmd, subcommand, user),
        "dump" => DumpTemplate(cmd, subcommand, user),
        "list" => ListTemplates(cmd, user),
        _ => throw new InvalidOperationException($"{Emotes.ErrorEmote} Unknown subcommand {subcommand.Name}")
      };

      await handle;
    }

    private bool Authorize(SocketGuildUser user, string subcommand, out string? error)
    {
      if (subcommand == "list")
      {
        return service.IsAuthorized(user, ModrankLevel.Moderator, out error);
      }

      return service.IsAuthorized(user, ModrankLevel.Administrator, out error);
    }

    private async Task AddTemplate(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuildUser user)
    {
      var name = subcommand.GetOption<string>("name")!;
      var channel = subcommand.GetOption<SocketTextChannel>("channel")!;
      var mention = subcommand.GetOption<SocketRole>("mention")!;
      var hidden = subcommand.GetOption<bool>("hidden")!;

      if (await service.HasTemplate(user.Guild, name))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} Template **{name}** already exists");
        return;
      }

      var t = new TemplateModel()
      {
        Name = name,
        Channel = channel,
        Mention = mention,
        Hidden = hidden,
        Creator = user,
      };

      var modal = CreateTemplateModal(t);
      await cmd.RespondWithModalAsync(modal.Build());

      var _ = HandleModalSubmission(modal, t);
    }

    private async Task HandleModalSubmission(SubmittableModalBuilder modal, TemplateModel t)
    {
      var submitted = await modal.WaitForSubmission();
      t.Title = submitted.GetValue(nameof(t.Title))!;
      t.Description = submitted.GetValue(nameof(t.Description))!;
      t.Footer = submitted.GetValue(nameof(t.Footer));
      t.ThumbnailImageUrl = submitted.GetValue(nameof(t.ThumbnailImageUrl));
      t.LargeImageUrl = submitted.GetValue(nameof(t.LargeImageUrl));

      if (!service.ValidateTemplateParameters(submitted, t))
      {
        return;
      }

      await service.AddTemplate(t);

      await submitted.RespondAsync($"{Emotes.SuccessEmote} Added template **{t.Name}**");
    }

    private SubmittableModalBuilder CreateTemplateModal(TemplateModel t)
    {
      return (SubmittableModalBuilder)new SubmittableModalBuilder()
        .WithTitle("Template: Embed creator")
        .AddTextInput("Title", nameof(t.Title), placeholder: "Title of the embed, Placeholders can be used here", required: true, maxLength: EmbedBuilder.MaxTitleLength)
        .AddTextInput("Description", nameof(t.Description), placeholder: "Description of the embed, Placeholders can be used here", required: true, maxLength: 2000)
        .AddTextInput("Footer", nameof(t.Footer), placeholder: "Footer text, Placeholders can be used here", required: false, maxLength: 2048)
        .AddTextInput("Thumbnail image URL", nameof(t.ThumbnailImageUrl), placeholder: "Image URL of thumbnail in the embed", required: false)
        .AddTextInput("Image URL (large image)", nameof(t.LargeImageUrl), placeholder: "Image URL (large image)", required: false);
    }

    private async Task RemoveTemplate(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuildUser user)
    {
      var name = subcommand.GetOption<string>("name")!;

      if (!await service.HasTemplate(user.Guild, name))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} Template **{name}** does not exist");
        return;
      }

      await service.RemoveTemplate(user.Guild, name);
      await cmd.RespondAsync($"{Emotes.SuccessEmote} Removed template **{name}**");
    }

    private async Task DumpTemplate(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuildUser user)
    {
      var name = subcommand.GetOption<string>("name")!;

      if (!await service.HasTemplate(user.Guild, name))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} Template **{name}** does not exist");
        return;
      }

      var t = (await service.GetTemplate(user.Guild, name))!;
      var dump = service.GetTemplateDump(t);
      if (dump.Length > EmbedBuilder.MaxEmbedLength)
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} You have reached the maximum embed character limit ({EmbedBuilder.MaxEmbedLength} characters), so the template cannot be dumped, try recreating the template but shorter");
        return;
      }

      await cmd.RespondAsync(text: $"Dump of template **{name}**:", embed: dump.Build());
    }

    private async Task ListTemplates(SocketSlashCommand cmd, SocketGuildUser user)
    {
      var guild = user.Guild;
      var templates = await service.ListTemplates(user.Guild);

      if (!await service.HasTemplates(guild))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} There are no templates");
        return;
      }

      var p = new PaginatableEmbedBuilder<(string Name, SocketGuildUser Creator)>
        (5, templates, items =>
          new EmbedBuilder()
            .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
            .WithTitle("Templates")
            .WithFields(items.Select(x => new EmbedFieldBuilder()
              .WithName(x.Name)
              .WithValue($"Creator: {x.Creator.Mention}")))
            .WithColor(Colors.Blurple)
        );

      await cmd.RespondAsync(embed: p.Embed, components: p.Components);
    }
  }
}
