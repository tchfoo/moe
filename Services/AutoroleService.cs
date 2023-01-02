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
    if (autoroles.Count == 0)
    {
      return;
    }

    var guild = user.Guild;
    var highestBotRole = guild.CurrentUser.Roles.OrderByDescending(x => x.Position).First();
    foreach (var role in autoroles.ToList())
    {
      if (role.Position > highestBotRole.Position)
      {
        await LogService.Instance.LogToDiscord(guild, $"{Emotes.ErrorEmote} Role {role.Mention} is in a higher position than my role ({highestBotRole.Mention}), therefore I can't apply this autorole to new users");
        autoroles.Remove(role);
      }
    }

    await LogService.LogToFileAndConsole(
      $"User joined, applying autoroles {string.Join(", ", autoroles)} to {user}", user.Guild);
    await user.AddRolesAsync(autoroles);
  }
}
