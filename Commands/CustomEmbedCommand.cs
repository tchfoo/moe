using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands;

public class CustomEmbedCommand : SlashCommandBase
{
  private readonly CustomEmbedService service;

  public CustomEmbedCommand(CustomEmbedService service) : base("embed")
  {
    Description = "Send a customizable embed";
    Options = new SlashCommandOptionBuilder()
      .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel where the embed should be sent. Defaults to current channel", isRequired: false, channelTypes: new List<ChannelType>() { ChannelType.Text });
    this.service = service;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    var user = (cmd.User as SocketGuildUser)!;
    var channel = cmd.GetOption<SocketTextChannel>("channel") ?? (cmd.Channel as SocketTextChannel)!;
    
    if(!service.IsAuthorized(user, ModrankLevel.Moderator, out var error))
    {
      await cmd.RespondAsync(error);
      return;
    }

    var customEmbed = new CustomEmbed()
    {
      Channel = channel,
    };
    var modal = CreateEmbedModal();
    await cmd.RespondWithModalAsync(modal.Build());

    var _ = HandleModalSubmission(modal, customEmbed);
  }

  private SubmittableModalBuilder CreateEmbedModal()
  {
    return (SubmittableModalBuilder)new SubmittableModalBuilder()
      .WithTitle("Embed creator")
      .AddTextInput("Title", nameof(CustomEmbed.Title), placeholder: "Title of the embed", required: true, maxLength: EmbedBuilder.MaxTitleLength)
      .AddTextInput("Description", nameof(CustomEmbed.Description), placeholder: "Description of the embed", required: true, maxLength: 2000, style: TextInputStyle.Paragraph)
      .AddTextInput("Footer", nameof(CustomEmbed.Footer), placeholder: "Footer text", required: false, maxLength: 2048)
      .AddTextInput("Thumbnail image URL", nameof(CustomEmbed.ThumbnailImageUrl), placeholder: "Image URL of thumbnail in the embed", required: false)
      .AddTextInput("Image URL (large image)", nameof(CustomEmbed.LargeImageUrl), placeholder: "Image URL (large image)", required: false);
  }

  private async Task HandleModalSubmission(SubmittableModalBuilder modal, CustomEmbed customEmbed)
  {
    var submitted = await modal.WaitForSubmission();
    var embed = GetEmbedToSend(submitted);
    await ShowEmbed(submitted, embed, customEmbed.Channel);
  }

  private async Task ShowEmbed(SocketInteraction interaction, EmbedBuilder embed, SocketTextChannel channel)
  {
    if (embed.Length > EmbedBuilder.MaxEmbedLength)
    {
      await interaction.RespondAsync($"{Emotes.ErrorEmote} You have reached the maximum embed character limit ({EmbedBuilder.MaxEmbedLength} characters), so the announcement cannot be displayed, try recreating the template but shorter");
      return;
    }

    await channel.SendMessageAsync(embed: embed.Build());
    await interaction.RespondAsync($"{Emotes.SuccessEmote} Embed sent");
  }

  private EmbedBuilder GetEmbedToSend(SocketModal modal)
  {
    return new EmbedBuilder()
      .WithTitle(modal.GetValue(nameof(CustomEmbed.Title))!)
      .WithDescription(modal.GetValue(nameof(CustomEmbed.Description))!)
      .WithFooter(modal.GetValue(nameof(CustomEmbed.Footer)))
      .WithThumbnailUrl(modal.GetValue(nameof(CustomEmbed.ThumbnailImageUrl)))
      .WithImageUrl(modal.GetValue(nameof(CustomEmbed.LargeImageUrl)))
      .WithColor(Colors.Blurple);
  }
}
