using Discord.WebSocket;
using Moe.Models;

namespace Moe.Services;

public class RngService
{
  private readonly SettingsService settingsService;

  public RngService(SettingsService settingsService)
  {
    this.settingsService = settingsService;
  }

  public bool IsAuthorizedDMSilent(SocketUser user, ModrankLevel requiredLevel)
  {
    return settingsService.IsAuthorizedDMSilent(user, requiredLevel);
  }
}
