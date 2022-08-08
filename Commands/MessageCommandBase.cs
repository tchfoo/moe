
using Discord;
using Discord.WebSocket;

namespace MoeBot.Commands;

public abstract class MessageCommandBase
{
  public string Name { get; protected set; }

  protected MessageCommandBase(string name)
  {
    Name = name;
  }

  public abstract Task Handle(SocketMessageCommand cmd);

  public MessageCommandProperties GetCommandProperties()
  {
    var builder = new MessageCommandBuilder()
      .WithName(Name);

    return builder.Build();
  }
}
