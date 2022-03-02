using Discord;
using Discord.WebSocket;

namespace TNTBot.Commands
{
  public class TemplateCommand : SlashCommandBase
  {
    public TemplateCommand() : base("template")
    {
      Description = "Create, remove or list /announce templates";
      Options = new SlashCommandOptionBuilder()
        .AddOption(new SlashCommandOptionBuilder()
          .WithName("create")
          .WithDescription("Create a template")
          .AddOption("name", ApplicationCommandOptionType.String, "The name of the template", isRequired: true)
          .AddOption("hidden", ApplicationCommandOptionType.Boolean, "Whether to hide the template from the template list, default = false")
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
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var subcommand = cmd.GetSubcommand();

      switch (subcommand.Name)
      {
        case "create":
          var modal = new ModalBuilder()
            .WithTitle("Template: Embed creator")
            .WithCustomId("template_embed_creator")
            .AddTextInput("Title", "embed_title", placeholder: "Title of the embed, Placeholders can be used here", required: true)
            .AddTextInput("Description", "embed_description", placeholder: "Description of the embed, Placeholders can be used here", required: true)
            .AddTextInput("Footer", "embed_footer", placeholder: "Footer text, Placeholders can be used here", required: false)
            .AddTextInput("Thumbnail image URL", "embed_thumbnail_image", placeholder: "Image URL of thumbnail in the embed", required: false)
            .AddTextInput("Image URL (large image)", "embed_image", placeholder: "Image URL (large image)", required: false);

          await cmd.RespondWithModalAsync(modal: modal.Build());
          break;
      }
    }
  }
}