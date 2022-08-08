using Discord;
using Discord.WebSocket;
using MoeBot.Services;

namespace MoeBot;

public class PaginatableEmbedBuilder<T>
{
  private readonly string backId = Guid.NewGuid().ToString();
  private readonly string pagesId = Guid.NewGuid().ToString();
  private readonly string forwardId = Guid.NewGuid().ToString();
  private readonly int itemsPerPage;
  private int currentPageNumber = 1;
  private readonly int maxPageNumber;
  private readonly IEnumerable<T> items;
  private readonly Func<IEnumerable<T>, EmbedBuilder> pageBuilder;

  public Embed Embed
  {
    get
    {
      var pageItems = items
        .Skip((currentPageNumber - 1) * itemsPerPage)
        .Take(itemsPerPage);
      return pageBuilder(pageItems).Build();
    }
  }

  public MessageComponent Components
  {
    get =>
      new ComponentBuilder()
        .WithButton("Back", backId, disabled: IsFirstPage())
        .WithButton($"{currentPageNumber}/{maxPageNumber}", pagesId, disabled: true)
        .WithButton("Forward", forwardId, disabled: IsLastPage())
        .Build();
  }

  public PaginatableEmbedBuilder(int itemsPerPage, IEnumerable<T> items, Func<IEnumerable<T>, EmbedBuilder> pageBuilder)
  {
    this.itemsPerPage = itemsPerPage;
    this.items = items;
    this.pageBuilder = pageBuilder;
    maxPageNumber = (int)Math.Ceiling(items.Count() / (double)itemsPerPage);
    maxPageNumber = Math.Max(1, maxPageNumber);
    DiscordService.Discord.ButtonExecuted += OnButtonExecuted;
  }

  ~PaginatableEmbedBuilder()
  {
    DiscordService.Discord.ButtonExecuted -= OnButtonExecuted;
  }

  private async Task OnButtonExecuted(SocketMessageComponent component)
  {
    var id = component.Data.CustomId;
    if (id == backId && !IsFirstPage())
    {
      currentPageNumber--;
    }
    else if (id == forwardId && !IsLastPage())
    {
      currentPageNumber++;
    }
    else
    {
      return;
    }

    await component.UpdateAsync(msg =>
    {
      msg.Embed = Embed;
      msg.Components = Components;
    });
  }

  private bool IsFirstPage() => currentPageNumber == 1;
  private bool IsLastPage() => currentPageNumber == maxPageNumber;
}
