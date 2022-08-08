
using Discord;
using Discord.WebSocket;
using MoeBot.Services;

namespace MoeBot;

public class SubmittableModalBuilder : ModalBuilder
{
  public delegate void SubmittedEventHandler(SocketModal modal);
  public event SubmittedEventHandler? OnSubmitted;

  public SubmittableModalBuilder()
  {
    CustomId = Guid.NewGuid().ToString();
    DiscordService.Discord.ModalSubmitted += OnModalSubmitted;
  }

  ~SubmittableModalBuilder()
  {
    DiscordService.Discord.ModalSubmitted -= OnModalSubmitted;
  }

  private Task OnModalSubmitted(SocketModal modal)
  {
    if (modal.Data.CustomId == CustomId)
    {
      OnSubmitted?.Invoke(modal);
    }

    return Task.CompletedTask;
  }
}
