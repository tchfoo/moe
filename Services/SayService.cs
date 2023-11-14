using Discord.WebSocket;
using Moe.Models;

namespace Moe.Services;

public class SayService
{
  private readonly SettingsService settingsService;

  public SayService(SettingsService settingsService)
  {
    this.settingsService = settingsService;
  }

  public bool IsAuthorized(SocketGuildUser user, ModrankLevel requiredLevel, out string? error)
  {
    return settingsService.IsAuthorized(user, requiredLevel, out error);
  }
}
