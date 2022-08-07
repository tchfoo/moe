using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands;

public class EditEmbedMessageCommand : MessageCommandBase
{
  private readonly CustomEmbedService service;

  public EditEmbedMessageCommand(CustomEmbedService service) : base("Edit embed")
  {
    this.service = service;
  }

  public override async Task Handle(SocketMessageCommand cmd)
  {
    var user = (cmd.User as SocketGuildUser)!;
    var channel = (cmd.Channel as SocketTextChannel)!;
    var message = cmd.Data.Message;
    var bot = channel.Guild.CurrentUser;

    if (!service.IsAuthorized(user, ModrankLevel.Moderator, out var error))
    {
      await cmd.RespondAsync(error);
      return;
    }

    if (message.Author.Id != bot.Id)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} You cannot edit that message");
      return;
    }

    if (message.Embeds.Count == 0)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} The message must be an embed");
      return;
    }

    var originalEmbed = message.Embeds.First();
    var customEmbed = new CustomEmbed()
    {
      Channel = channel,
      Title = originalEmbed.Title,
      Description = originalEmbed.Description,
      Footer = originalEmbed.Footer?.Text ?? "",
      ThumbnailImageUrl = originalEmbed.Thumbnail?.Url ?? "",
      LargeImageUrl = originalEmbed.Image?.Url ?? "",
    };

    var modal = EditEmbedModal(customEmbed);
    await cmd.RespondWithModalAsync(modal.Build());

    var _ = HandleModalSubmission(modal, customEmbed, message);
  }

  private SubmittableModalBuilder EditEmbedModal(CustomEmbed e)
  {
    return (SubmittableModalBuilder)new SubmittableModalBuilder()
      .WithTitle("Embed editor")
      .AddTextInput("Title", nameof(e.Title), placeholder: e.Title, value: e.Title, required: true, maxLength: EmbedBuilder.MaxTitleLength)
      .AddTextInput("Description", nameof(e.Description), placeholder: e.Description, value: e.Description, required: true, maxLength: 2000, style: TextInputStyle.Paragraph)
      .AddTextInput("Footer", nameof(e.Footer), placeholder: e.Footer, value: e.Footer, required: false, maxLength: 2048)
      .AddTextInput("Thumbnail image URL", nameof(e.ThumbnailImageUrl), placeholder: e.ThumbnailImageUrl, value: e.ThumbnailImageUrl, required: false)
      .AddTextInput("Image URL (large image)", nameof(e.LargeImageUrl), placeholder: e.LargeImageUrl, value: e.LargeImageUrl, required: false);
  }

  private async Task HandleModalSubmission(SubmittableModalBuilder modal, CustomEmbed customEmbed, SocketMessage message)
  {
    var submitted = await modal.WaitForSubmission();
    var embed = GetEmbedToSend(submitted);
    await EditEmbed(submitted, embed, customEmbed.Channel, message);
  }

  private async Task EditEmbed(SocketInteraction interaction, EmbedBuilder embed, SocketTextChannel channel, SocketMessage message)
  {
    if (embed.Length > EmbedBuilder.MaxEmbedLength)
    {
      await interaction.RespondAsync($"{Emotes.ErrorEmote} You have reached the maximum embed character limit ({EmbedBuilder.MaxEmbedLength} characters), so the announcement cannot be displayed, try recreating the template but shorter");
      return;
    }

    await channel.ModifyMessageAsync(message.Id, messageProperties =>
    {
      messageProperties.Embed = embed.Build();
    });
    await interaction.RespondAsync($"{Emotes.SuccessEmote} Embed edited");
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
