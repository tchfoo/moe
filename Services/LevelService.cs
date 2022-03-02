using Discord;
using Discord.WebSocket;
using TNTBot.Models;

namespace TNTBot.Services
{
  public class LevelService
  {
    private static readonly TimeSpan TimeBetweenMessages = TimeSpan.FromMinutes(1);

    public LevelService()
    {
      CreateLevelsTable().Wait();
    }

    public async Task HandleMessage(SocketMessage msg)
    {
      if (msg.Channel.GetChannelType() != ChannelType.Text)
      {
        return;
      }

      var user = (SocketGuildUser)msg.Author;
      await EnsureLevelExists(user);

      var levelBefore = await GetLevel(user);
      await TryAddingXP(user);
      var levelAfter = await GetLevel(user);
      for (int i = levelBefore.LevelNumber; i < levelAfter.LevelNumber; i++)
      {
        if (await IsLevelupMessageEnabled(user))
        {
          await msg.Channel.SendMessageAsync($"{user.Mention} has leveled up to level {i + 1}!");
        }
      }
    }

    public async Task<Level> GetLevel(SocketGuildUser user)
    {
      var sql = "SELECT id, xp, last_updated FROM levels WHERE guild_id = $0 AND user_id = $1";
      var result = (await DatabaseService.Query<int, int, DateTime>(sql, user.Guild.Id, user.Id))[0];
      return new Level(result.Item1, user, result.Item2, result.Item3);
    }

    public async Task<List<Level>> GetLeaderboard(SocketGuild guild)
    {
      var sql = "SELECT id, user_id, xp, last_updated FROM levels WHERE guild_id = $0 ORDER BY xp DESC";
      var results = await DatabaseService.Query<int, ulong, int, DateTime>(sql, guild.Id);
      return results.ConvertAll(x => new Level(x.Item1, guild.GetUser(x.Item2), x.Item3, x.Item4));
    }

    public async Task<int> GetRank(SocketGuildUser user)
    {
      var sql = "SELECT user_id FROM levels WHERE guild_id = $0 ORDER BY xp DESC";
      var ids = await DatabaseService.Query<ulong>(sql, user.Guild.Id);
      for (int i = 0; i < ids.Count; i++)
      {
        if (ids[i] == user.Id)
        {
          return i + 1;
        }
      }
      return -1;
    }

    public async Task EnsureLevelExists(SocketGuildUser user)
    {
      if (!await HasLevel(user))
      {
        await InsertLevel(user);
      }
    }

    public async Task ToggleLevelupMessage(SocketGuildUser user)
    {
      var enabled = await IsLevelupMessageEnabled(user);
      var enabledString = enabled ? "disabling" : "enabling";
      await LogService.LogToFileAndConsole(
        $"User {user} is {enabledString} their levelup messages", user.Guild);

      await SetLevelupMessageEnabled(user, !enabled);
    }

    public async Task<bool> IsLevelupMessageEnabled(SocketGuildUser user)
    {
      var sql = "SELECT levelup_message FROM levels WHERE guild_id = $0 AND user_id = $1";
      return await DatabaseService.QueryFirst<int>(sql, user.Guild.Id, user.Id) > 0;
    }

    private async Task SetLevelupMessageEnabled(SocketGuildUser user, bool enabled)
    {
      var sql = "UPDATE levels SET levelup_message = $0 WHERE guild_id = $1 AND user_id = $2";
      await DatabaseService.NonQuery(sql, enabled, user.Guild.Id, user.Id);
    }

    private async Task TryAddingXP(SocketGuildUser user)
    {
      var level = await GetLevel(user);
      var nextUpdate = level.LastUpdated + TimeBetweenMessages;
      var shouldUpdate = DateTime.Now >= nextUpdate;
      if (shouldUpdate)
      {
        var xp = GenerateXP();
        await AddXP(user, xp);
      }
    }

    private int GenerateXP()
    {
      return Random.Shared.Next(Level.MinXPPerMessage, Level.MaxXPPerMessage);
    }

    private async Task AddXP(SocketGuildUser user, int xp)
    {
      var level = await GetLevel(user);
      await UpdateLevel(user, level.TotalXP + xp);
    }

    private async Task<bool> HasLevel(SocketGuildUser user)
    {
      var sql = "SELECT COUNT(*) FROM levels WHERE guild_id = $0 AND user_id = $1";
      var count = await DatabaseService.QueryFirst<int>(sql, user.Guild.Id, user.Id);
      return count > 0;
    }

    private async Task InsertLevel(SocketGuildUser user)
    {
      var sql = "INSERT INTO levels(guild_id, user_id) VALUES($0, $1)";
      await DatabaseService.NonQuery(sql, user.Guild.Id, user.Id);
    }

    private async Task UpdateLevel(SocketGuildUser user, int xp)
    {
      var sql = "UPDATE levels SET xp = $0, last_updated = $1 WHERE guild_id = $2 AND user_id = $3";
      await DatabaseService.NonQuery(sql, xp, DateTime.Now, user.Guild.Id, user.Id);
    }

    private async Task CreateLevelsTable()
    {
      var sql = $@"
        CREATE TABLE IF NOT EXISTS levels (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          user_id INTEGER NOT NULL,
          xp INTEGER NOT NULL DEFAULT 0,
          levelup_message INTEGER NOT NULL DEFAULT 1,
          last_updated TEXT NOT NULL DEFAULT '{DateTime.MinValue}'
        )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
