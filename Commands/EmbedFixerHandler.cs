using Discord.WebSocket;
using Moe.Services;

namespace Moe.Commands;

public class EmbedFixerHandler
{
  private readonly EmbedFixerService service;

  public EmbedFixerHandler(EmbedFixerService service)
  {
    this.service = service;
  }

  public async Task<bool> TryHandleMessage(SocketMessage message)
  {
    var channel = message.Channel;
    var guild = (channel as SocketGuildChannel)!.Guild;
    var content = message.Content;

    var fixedContent = await service.ReplaceLinks(guild.Id, content);

    if (content == fixedContent)
    {
      return false;
    }

    var sendMessageTask = channel.SendMessageAsync($"{fixedContent} | Sent by {message.Author.Mention}");
    var deleteMessageTask = message.DeleteAsync();

    await sendMessageTask;
    await deleteMessageTask;

    return true;
  }
}
