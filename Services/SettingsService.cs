using Discord.WebSocket;

namespace TNTBot.Services
{
  public class SettingsService
  {
    public SettingsService()
    {
      CreateSettingsTable().Wait();
      CreateModranksTable().Wait();
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

    public async Task<List<(SocketRole Role, int Level)>> GetModranks(SocketGuild guild)
    {
      var sql = "SELECT role_id, level FROM modranks WHERE guild_id = $0";
      var result = await DatabaseService.Query<ulong, int>(sql, guild.Id);
      return result.ConvertAll(x => (guild.GetRole(x.Item1), x.Item2));
    }

    public bool IsAuthorized(SocketGuildUser user, int modrankLevel, out string? error)
    {
      error = null;
      if (modrankLevel < GetModrankLevel(user).Result)
      {
        error = $"This action requires at least {ConvertModrankLevelToString(modrankLevel)} modrank";
        return false;
      }

      return true;
    }

    public async Task SetModrank(SocketRole role, int level)
    {
      await RemoveModrank(role);
      if (level > 0)
      {
        await AddModrank(role, level);
      }
    }

    public string ConvertModrankLevelToString(int level)
    {
      return level switch
      {
        0 => "None",
        1 => "Moderator",
        2 => "Administrator",
        3 => "Owner",
        _ => "Unknown"
      };
    }

    private async Task<int> GetModrankLevel(SocketGuildUser user)
    {
      if (ConfigService.Config.Owners.Contains(user.Id))
      {
        return 3;
      }

      var modranks = await GetModranksForUser(user);
      if (user.GuildPermissions.Administrator)
      {
        return 2;
      }
      if (modranks.Any(x => x.Level == 2))
      {
        return 2;
      }

      if (modranks.Any(x => x.Level == 1))
      {
        return 1;
      }

      return 0;
    }

    private async Task<List<(SocketRole Role, int Level)>> GetModranksForUser(SocketGuildUser user)
    {
      var modranks = await GetModranks(user.Guild);
      return modranks.Where(x => user.Roles.Contains(x.Role)).ToList();
    }

    private async Task AddModrank(SocketRole role, int level)
    {
      await LogService.LogToFileAndConsole(
        $"Adding modrank for role {role} with level {level}", role.Guild);

      var sql = "INSERT INTO modranks(guild_id, role_id, level) VALUES($0, $1, $2)";
      await DatabaseService.NonQuery(sql, role.Guild.Id, role.Id, level);
    }

    private async Task RemoveModrank(SocketRole role)
    {
      await LogService.LogToFileAndConsole(
        $"Removing modrank for role {role}", role.Guild);

      var sql = "DELETE FROM modranks WHERE guild_id = $0 AND role_id = $1";
      await DatabaseService.NonQuery(sql, role.Guild.Id, role.Id);
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

    private async Task CreateModranksTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS modranks(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          role_id INTEGER NOT NULL,
          level INTEGER NOT NULL
        )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
