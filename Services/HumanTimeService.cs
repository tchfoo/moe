
using Discord.WebSocket;
using TNTBot.Models;

namespace TNTBot.Services
{
  public class HumanTimeService
  {
    private readonly SettingsService settingsService;

    public HumanTimeService(SettingsService settingsService)
    {
      this.settingsService = settingsService;
    }

    public async Task<TimeZoneTime> ParseTimeZoneTime(SocketGuild guild, string input)
    {
      var defaultTimeZone = await settingsService.GetTimeZone(guild);
      return TimeZoneTime.Parse(input, defaultTimeZone);
    }
  }
}
