using Discord.WebSocket;

namespace TNTBot.Services
{
  public class SettingsService
  {
    public SettingsService()
    {
      CreateSettingsTable().Wait();
    }

    public async Task<SocketTextChannel?> GetPinChannel(SocketGuild guild)
    {
      var sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'pin_channel'";
      var channelId = await DatabaseService.QueryFirst<ulong>(sql, guild.Id);
      if (channelId == 0)
      {
        return null;
      }
      return guild.GetTextChannel(channelId);
    }

    public async Task SetPinChannel(SocketGuild guild, SocketTextChannel channel)
    {
      await LogService.LogToFileAndConsole(
        $"Setting pin channel to {channel}", guild);

      var deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'pin_channel'";
      await DatabaseService.NonQuery(deleteSql, guild.Id);
      var insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'pin_channel', $1)";
      await DatabaseService.NonQuery(insertSql, guild.Id, channel.Id);
    }

    public async Task<bool> HasPinChannel(SocketGuild guild)
    {
      return await GetPinChannel(guild) != null;
    }

    public async Task<SocketTextChannel?> GetLogChannel(SocketGuild guild)
    {
      var sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'log_channel'";
      var channelId = await DatabaseService.QueryFirst<ulong>(sql, guild.Id);
      if (channelId == 0)
      {
        return null;
      }
      return guild.GetTextChannel(channelId);
    }

    public async Task SetLogChannel(SocketGuild guild, SocketTextChannel channel)
    {
      await LogService.LogToFileAndConsole(
        $"Setting log channel to {channel}", guild);

      var deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'log_channel'";
      await DatabaseService.NonQuery(deleteSql, guild.Id);
      var insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'log_channel', $1)";
      await DatabaseService.NonQuery(insertSql, guild.Id, channel.Id);
    }

    public async Task<bool> HasLogChannel(SocketGuild guild)
    {
      return await GetLogChannel(guild) != null;
    }

    public async Task<TimeSpan> GetMuteLength(SocketGuild guild)
    {
      var sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'mute_length'";
      var muteLength = await DatabaseService.QueryFirst<string>(sql, guild.Id);
      if (muteLength is null)
      {
        return TimeSpan.FromMinutes(30);
      }
      return TimeSpan.Parse(muteLength);
    }

    public async Task SetMuteLength(SocketGuild guild, TimeSpan length)
    {
      await LogService.LogToFileAndConsole(
        $"Setting mute length to {length}", guild);

      var deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'mute_length'";
      await DatabaseService.NonQuery(deleteSql, guild.Id);
      var insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'mute_length', $1)";
      await DatabaseService.NonQuery(insertSql, guild.Id, length);
    }

    private async Task CreateSettingsTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS settings(
        guild_id INTEGER NOT NULL,
        name TEXT NOT NULL,
        value TEXT NOT NULL
      )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
