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
        await msg.Channel.SendMessageAsync($"{user.Mention} leveled up to level {i}!");
      }
    }

    private async Task EnsureLevelExists(SocketGuildUser user)
    {
      if (!await HasLevel(user))
      {
        await InsertLevel(user);
      }
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
      await UpdateLevel(user, level.XP + xp);
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

    private async Task<Level> GetLevel(SocketGuildUser user)
    {
      var sql = "SELECT id, xp, last_updated FROM levels WHERE guild_id = $0 AND user_id = $1";
      var result = (await DatabaseService.Query<int, int, DateTime>(sql, user.Guild.Id, user.Id))[0];
      return new Level(result.Item1, user, result.Item2, result.Item3);
    }

    private async Task UpdateLevel(SocketGuildUser user, int xp)
    {
      var sql = "UPDATE levels SET xp = $0 WHERE guild_id = $1 AND user_id = $2";
      await DatabaseService.NonQuery(sql, xp, user.Guild.Id, user.Id);
    }

    private async Task CreateLevelsTable()
    {
      var sql = $@"
        CREATE TABLE IF NOT EXISTS levels (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          user_id INTEGER NOT NULL,
          xp INTEGER NOT NULL DEFAULT 0,
          last_updated TEXT NOT NULL DEFAULT '{DateTime.MinValue}'
        )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
