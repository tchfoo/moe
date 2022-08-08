using Discord.WebSocket;

namespace MoeBot;

public static class SocketModalExtension
{
  public static string? GetValue(this SocketModal modal, string id, string? @default = default)
  {
    var value = modal.Data.Components.First(x => x.CustomId == id)?.Value;
    if (string.IsNullOrEmpty(value))
    {
      return @default;
    }
    return value;
  }
}
