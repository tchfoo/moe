using Discord.WebSocket;

namespace MoeBot.Services;

public class AutoroleService
{
  private readonly SettingsService settingsService;

  public AutoroleService(SettingsService settingsService)
  {
    this.settingsService = settingsService;
  }

  public void Register()
  {
    DiscordService.Discord.UserJoined += OnUserJoined;
  }

  private async Task OnUserJoined(SocketGuildUser user)
  {
    var autoroles = await settingsService.GetAutoroles(user.Guild);
    if(autoroles.Count == 0)
    {
      return;
    }

    await user.AddRolesAsync(autoroles);

    await LogService.LogToFileAndConsole(
      $"User joined, adding autoroles {string.Join(", ", autoroles)} to {user}", user.Guild);
  }
}
