using Discord;
using Discord.WebSocket;
using MoeBot.Services;

namespace MoeBot;

public class SubmittableSelectMenuBuilder : SelectMenuBuilder
{
  public delegate void SubmittedEventHandler(SocketMessageComponent component);
  public event SubmittedEventHandler? OnSubmitted;

  public SubmittableSelectMenuBuilder()
  {
    CustomId = Guid.NewGuid().ToString();
    DiscordService.Discord.SelectMenuExecuted += OnSelectMenuExecuted;
  }

  ~SubmittableSelectMenuBuilder()
  {
    DiscordService.Discord.SelectMenuExecuted -= OnSelectMenuExecuted;
  }

  private Task OnSelectMenuExecuted(SocketMessageComponent component)
  {
    if (component.Data.CustomId == CustomId)
    {
      OnSubmitted?.Invoke(component);
    }

    return Task.CompletedTask;
  }
}
