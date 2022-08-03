using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands;

public class PinMessageCommand : MessageCommandBase
{
  private readonly PinService service;

  public PinMessageCommand(PinService service) : base("Pin to pin channel")
  {
    this.service = service;
  }

  public override async Task Handle(SocketMessageCommand cmd)
  {
    await service.TryPinningMessage(cmd, cmd.Data.Message);
  }
}
