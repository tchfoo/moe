using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot;

public class SubmittableModalBuilder : ModalBuilder
{
  private SocketModal? submittedModal;
  private readonly AutoResetEvent submittedEvent;

  public SubmittableModalBuilder()
  {
    CustomId = Guid.NewGuid().ToString();
    DiscordService.Discord.ModalSubmitted += OnSubmitted;
    submittedEvent = new AutoResetEvent(false);
  }

  ~SubmittableModalBuilder()
  {
    DiscordService.Discord.ModalSubmitted -= OnSubmitted;
  }

  public async Task<SocketModal> WaitForSubmission()
  {
    await Task.Run(() => submittedEvent.WaitOne());
    return submittedModal!;
  }

  private Task OnSubmitted(SocketModal modal)
  {
    if (modal.Data.CustomId == CustomId)
    {
      submittedModal = modal;
      submittedEvent.Set();
    }

    return Task.CompletedTask;
  }
}
