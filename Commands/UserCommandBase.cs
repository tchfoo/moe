
using Discord;
using Discord.WebSocket;

namespace Moe.Commands;

public abstract class UserCommandBase
{
  public string Name { get; protected set; }

  protected UserCommandBase(string name)
  {
    Name = name;
  }

  public abstract Task Handle(SocketUserCommand cmd);

  public UserCommandProperties GetCommandProperties()
  {
    var builder = new UserCommandBuilder()
      .WithName(Name);

    return builder.Build();
  }
}
