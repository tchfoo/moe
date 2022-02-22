using Discord;
using Discord.WebSocket;

namespace TNTBot.Services
{
  public class MuteService
  {
    private const string MutedRoleName = "muted-by-tntbot";
    private readonly Dictionary<int, CancellationTokenSource> unmuteTasks;
    private readonly SettingsService settingsService;

    public MuteService(SettingsService settingsService)
    {
      unmuteTasks = new Dictionary<int, CancellationTokenSource>();
      CreateMutesTable().Wait();
      LoadMutes().Wait();
      this.settingsService = settingsService;
    }

    public async Task MuteUser(SocketGuildUser user, DateTime expireAt)
    {
      await LogService.LogToFileAndConsole(
        $"Muting user {user}, expires at {expireAt}", user.Guild);

      var mutedRole = await GetMutedRole(user.Guild);
      await user.AddRoleAsync(mutedRole);

      var muteSql = "INSERT INTO mutes(guild_id, user_id, expire_at) VALUES($0, $1, $2)";
      await DatabaseService.NonQuery(muteSql, user.Guild.Id, user.Id, expireAt);

      var duration = expireAt - DateTime.Now;
      await StartUnmuteTimer(user, duration);
    }

    public async Task UnmuteUser(SocketGuildUser user)
    {
      await LogService.LogToFileAndConsole(
        $"Unmuted user {user}", user.Guild);

      var mutedRole = await GetMutedRole(user.Guild);
      await user.RemoveRoleAsync(mutedRole);

      await StopUnmuteTimer(user);

      var unmuteSql = "DELETE FROM mutes WHERE guild_id = $0 AND user_id = $1";
      await DatabaseService.NonQuery(unmuteSql, user.Guild.Id, user.Id);
    }

    public async Task<TimeSpan> GetDefaultMuteLength(SocketGuild guild)
    {
      return await settingsService.GetMuteLength(guild);
    }

    private async Task CreateMutesTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS mutes(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        guild_id INTEGER NOT NULL,
        user_id INTEGER NOT NULL,
        expire_at TEXT NOT NULL
      )";
      await DatabaseService.NonQuery(sql);
    }

    private async Task LoadMutes()
    {
      var mutes = await DatabaseService.Query<ulong, ulong, DateTime>("SELECT guild_id, user_id, expire_at FROM mutes");
      foreach (var mute in mutes)
      {
        var guildId = mute.Item1;
        var userId = mute.Item2;
        var expireAt = mute.Item3;
        var duration = expireAt - DateTime.Now;

        var guild = DiscordService.Discord.GetGuild(guildId);
        await guild.DownloadUsersAsync();
        var user = guild.GetUser(userId);

        await StartUnmuteTimer(user, duration);
      }
    }

    private async Task CreateMutedRole(SocketGuild guild)
    {
      var perms = new GuildPermissions(sendMessages: false);
      await guild.CreateRoleAsync(MutedRoleName, perms, Color.DarkRed, false, null);
    }

    private async Task<SocketRole> GetMutedRole(SocketGuild guild)
    {
      if (!guild.Roles.Any(x => x.Name == MutedRoleName))
      {
        await CreateMutedRole(guild);
      }
      return guild.Roles.First(x => x.Name == MutedRoleName);
    }

    public async Task<bool> IsMuted(SocketGuildUser user)
    {
      var mutesCountSql = "SELECT COUNT(*) FROM mutes WHERE guild_id = $0 AND user_id = $1";
      int mutesCount = await DatabaseService.QueryFirst<int>(mutesCountSql, user.Guild.Id, user.Id);
      return mutesCount > 0;
    }

    private Task<int> GetMuteId(SocketGuildUser user)
    {
      var muteIdSql = "SELECT id FROM mutes WHERE guild_id = $0 AND user_id = $1";
      return DatabaseService.QueryFirst<int>(muteIdSql, user.Guild.Id, user.Id);
    }

    private async Task StartUnmuteTimer(SocketGuildUser user, TimeSpan duration)
    {
      var muteId = await GetMuteId(user);
      var tokenSource = new CancellationTokenSource();
      var unmuteAction = async () => await UnmuteTimer(user, duration);
      var _ = Task.Run(unmuteAction, tokenSource.Token);
      unmuteTasks.Add(muteId, tokenSource);
    }

    private async Task StopUnmuteTimer(SocketGuildUser user)
    {
      var muteId = await GetMuteId(user);
      var unmuteTask = unmuteTasks[muteId];
      unmuteTasks.Remove(muteId);
      unmuteTask.Cancel();
    }

    private async Task UnmuteTimer(SocketGuildUser user, TimeSpan duration)
    {
      if (duration.TotalMilliseconds > 0)
      {
        await Task.Delay((int)duration.TotalMilliseconds);
      }

      await LogService.LogToFileAndConsole(
        $"Mute is expired for user {user}", user.Guild);
      await UnmuteUser(user);
    }
  }
}
