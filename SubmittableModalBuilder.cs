
using Discord;
using Discord.WebSocket;
using Moe.Services;

namespace Moe;

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
