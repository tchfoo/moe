using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class TemplateCommand : SlashCommandBase
  {
    private readonly TemplateService service;
    private readonly Dictionary<string, (SocketGuildUser Creator, string Name, SocketTextChannel Channel, SocketRole Mention, bool Hidden)> pendingModals;

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
          .WithName("list")
          .WithDescription("List templates")
          .WithType(ApplicationCommandOptionType.SubCommand)
      ).WithType(ApplicationCommandOptionType.SubCommandGroup);
      this.service = service;
      pendingModals = new Dictionary<string, (SocketGuildUser creator, string name, SocketTextChannel channel, SocketRole mention, bool hidden)>();
      DiscordService.Discord.ModalSubmitted += OnModalSubmitted;
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
        "list" => ListTemplates(cmd, user),
        _ => throw new InvalidOperationException($"Unknown subcommand {subcommand.Name}")
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
        await cmd.RespondAsync($"Template **{name}** already exists");
        return;
      }

      var modalId = Guid.NewGuid().ToString();
      pendingModals.Add(modalId, (user, name, channel, mention, hidden));

      var modal = new ModalBuilder()
        .WithTitle("Template: Embed creator")
        .WithCustomId(modalId)
        .AddTextInput("Title", "embed_title", placeholder: "Title of the embed, Placeholders can be used here", required: true)
        .AddTextInput("Description", "embed_description", placeholder: "Description of the embed, Placeholders can be used here", required: true)
        .AddTextInput("Footer", "embed_footer", placeholder: "Footer text, Placeholders can be used here", required: false)
        .AddTextInput("Thumbnail image URL", "embed_thumbnail_image", placeholder: "Image URL of thumbnail in the embed", required: false)
        .AddTextInput("Image URL (large image)", "embed_image", placeholder: "Image URL (large image)", required: false);

      await cmd.RespondWithModalAsync(modal: modal.Build());
    }

    private async Task OnModalSubmitted(SocketModal modal)
    {
      var id = modal.Data.CustomId;
      if (!pendingModals.TryGetValue(id, out var data))
      {
        return;
      }

      var (creator, name, channel, mention, hidden) = data;
      var title = modal.GetValue("embed_title")!;
      var description = modal.GetValue("embed_description")!;
      var footer = modal.GetValue("embed_footer");
      var thumbnailImage = modal.GetValue("embed_thumbnail_image");
      var image = modal.GetValue("embed_image");

      if (!service.ValidateTemplateParameters(modal, title, description, footer, thumbnailImage, image))
      {
        return;
      }

      await service.AddTemplate(creator, name, channel, mention, hidden, title!, description!, footer, thumbnailImage, image);

      await modal.RespondAsync($"Added template **{name}**");
      pendingModals.Remove(id);
    }

    private async Task RemoveTemplate(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuildUser user)
    {
      var name = subcommand.GetOption<string>("name")!;

      if (!await service.HasTemplate(user.Guild, name))
      {
        await cmd.RespondAsync($"Template **{name}** does not exist");
        return;
      }

      await service.RemoveTemplate(user.Guild, name);
      await cmd.RespondAsync($"Removed template **{name}**");
    }

    private async Task ListTemplates(SocketSlashCommand cmd, SocketGuildUser user)
    {
      var guild = user.Guild;
      var templates = await service.ListTemplates(user.Guild);

      if (!await service.HasTemplates(guild))
      {
        await cmd.RespondAsync("There are no templates");
        return;
      }

      var embed = new EmbedBuilder()
        .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
        .WithTitle("Templates")
        .WithColor(Colors.Blurple);

      foreach (var template in templates)
      {
        embed.AddField(template.Name, $"Creator: {template.Creator.Mention}");
      }

      await cmd.RespondAsync(embed: embed.Build());
    }
  }
}
