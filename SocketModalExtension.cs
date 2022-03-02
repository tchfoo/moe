using Discord.WebSocket;

namespace TNTBot
{
  public static class SocketModalExtension
  {
    public static string? GetValue(this SocketModal modal, string id, string? @default = default)
    {
      if (modal.Data.Components.Any(x => x.CustomId == id))
      {
        return modal.Data.Components.First(x => x.CustomId == id).Value;
      }
      return @default;
    }
  }
}
