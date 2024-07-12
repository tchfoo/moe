using Discord.WebSocket;
using Moe.Services;

namespace Moe.Commands;

public class PinMessageCommand : MessageCommandBase
{
  private readonly PinService service;

  public PinMessageCommand(PinService service) : base("Pin to pin channel")
  {
    this.service = service;
  }

  public override async Task Handle(SocketMessageCommand cmd)
  {
    await cmd.DeferAsync();

    await service.TryPinningMessage(cmd, cmd.Data.Message);
  }
}
