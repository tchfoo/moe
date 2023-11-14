using Discord;
using Discord.WebSocket;
using Moe.Models;

namespace Moe.Services;

public class PurgeService
{
  private readonly SettingsService settingsService;

  public readonly int MaxPurgeCount = 150;

  public PurgeService(SettingsService settingsService)
  {
    this.settingsService = settingsService;
  }

  public bool IsAuthorized(SocketGuildUser user, ModrankLevel requiredLevel, out string? error)
  {
    return settingsService.IsAuthorized(user, requiredLevel, out error);
  }

  public async Task Purge(SocketSlashCommand cmd, SocketTextChannel channel, int count)
  {
    await LogService.LogToFileAndConsole(
      $"Purging {count} messages from {channel}", channel.Guild);

    var messages = (await channel.GetMessagesAsync(count + 1)
      .FlattenAsync())
      .Where(x => x.Interaction?.Id != cmd.Id)
      .Take(count);
    await channel.DeleteMessagesAsync(messages);
  }
}
