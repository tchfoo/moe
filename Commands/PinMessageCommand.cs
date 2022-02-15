using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class PinMessageCommand : MessageCommandBase
  {
    private readonly PinService service;

    public PinMessageCommand(PinService service) : base("Pin to pin channel")
    {
      this.service = service;
    }

    public override async Task Handle(SocketMessageCommand cmd)
    {
      var pinChannel = service.GetPinChannel();
      var pinEmbed = service.PinMessageEmbed(cmd.Data.Message);

      await pinChannel.SendMessageAsync(embed: pinEmbed);
      await cmd.RespondAsync("Message successfully pinned.");
    }
  }
}
