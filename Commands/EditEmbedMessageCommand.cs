using Discord;
using Discord.WebSocket;
using Moe.Models;
using Moe.Services;

namespace Moe.Commands;

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
    modal.OnSubmitted += async submitted =>
    {
      var embed = GetEmbedToSend(submitted);
      await EditEmbed(submitted, embed, customEmbed.Channel, message);
    };

    await cmd.RespondWithModalAsync(modal.Build());
  }

  private SubmittableModalBuilder EditEmbedModal(CustomEmbed e)
  {
    return (SubmittableModalBuilder)new SubmittableModalBuilder()
      .WithTitle("Embed editor")
      .AddTextInput("Title", nameof(e.Title), placeholder: TrimPlaceholder(e.Title), value: e.Title, required: true, maxLength: EmbedBuilder.MaxTitleLength)
      .AddTextInput("Description", nameof(e.Description), placeholder: TrimPlaceholder(e.Description), value: e.Description, required: true, maxLength: 2000, style: TextInputStyle.Paragraph)
      .AddTextInput("Footer", nameof(e.Footer), placeholder: TrimPlaceholder(e.Footer), value: e.Footer, required: false, maxLength: EmbedFooterBuilder.MaxFooterTextLength)
      .AddTextInput("Thumbnail image URL", nameof(e.ThumbnailImageUrl), placeholder: TrimPlaceholder(e.ThumbnailImageUrl), value: e.ThumbnailImageUrl, required: false)
      .AddTextInput("Image URL (large image)", nameof(e.LargeImageUrl), placeholder: TrimPlaceholder(e.LargeImageUrl), value: e.LargeImageUrl, required: false);
  }

  private string? TrimPlaceholder(string? placeholder)
  {
    if (placeholder?.Length > TextInputBuilder.MaxPlaceholderLength)
    {
      return placeholder.Substring(0, Math.Min(placeholder.Length, 96)) + "...";
    }

    return placeholder;
  }

  private async Task EditEmbed(SocketInteraction interaction, EmbedBuilder embed, SocketTextChannel channel, SocketMessage message)
  {
    await interaction.DeferAsync();

    if (embed.Length > EmbedBuilder.MaxEmbedLength)
    {
      await interaction.FollowupAsync($"{Emotes.ErrorEmote} You have reached the maximum embed character limit ({EmbedBuilder.MaxEmbedLength} characters), so the announcement cannot be displayed, try recreating the template but shorter");
      return;
    }

    await channel.ModifyMessageAsync(message.Id, messageProperties =>
    {
      messageProperties.Embed = embed.Build();
    });
    await interaction.FollowupAsync($"{Emotes.SuccessEmote} Embed edited");
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
