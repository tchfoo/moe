using Discord.WebSocket;

namespace TNTBot.Services
{
  public class UserInfoService
  {
    public UserInfoService()
    {
      CreateFirstJoinedTable().Wait();
      DiscordService.Discord.UserJoined += OnUserJoined;
    }

    public async Task<DateTime?> FirstJoined(SocketGuildUser user)
    {
      if (!await HasFirstJoined(user))
      {
        return null;
      }

      return await GetFirstJoined(user);
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
      if (await HasFirstJoined(user))
      {
        return;
      }

      await AddUser(user);
    }

    private async Task AddUser(SocketGuildUser user)
    {
      var sql = @"
        INSERT INTO first_joined (guild_id, user_id, first_joined_at)
        VALUES($0, $1, $2)";
      await DatabaseService.NonQuery(sql, user.Guild.Id, user.Id, DateTime.Now);
    }

    private async Task<DateTime> GetFirstJoined(SocketGuildUser user)
    {
      var sql = "SELECT first_joined_at FROM first_joined WHERE guild_id = $0 AND user_id = $1";
      return await DatabaseService.QueryFirst<DateTime>(sql, user.Guild.Id, user.Id);
    }

    private async Task<bool> HasFirstJoined(SocketGuildUser user)
    {
      var sql = "SELECT COUNT(*) FROM first_joined WHERE guild_id = $0 AND user_id = $1";
      return await DatabaseService.QueryFirst<int>(sql, user.Guild.Id, user.Id) > 0;
    }

    private async Task CreateFirstJoinedTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS first_joined (
          Id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          user_id INTEGER NOT NULL,
          first_joined_at TEXT NOT NULL
        )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
