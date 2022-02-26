using Discord.WebSocket;

namespace TNTBot.Services
{
  public class RoleRememberService
  {
    public async Task Register()
    {
      DiscordService.Discord.UserLeft += OnUserLeft;
      DiscordService.Discord.UserJoined += OnUserJoined;
      await CreateRememberRolesTable();
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
      var guildUser = (SocketGuildUser)user;
      var roles = guildUser.Roles.Where(x => !x.IsEveryone);
      await SaveRoles(guildUser, roles);
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
      var roles = await LoadRoles(user);
      await user.AddRolesAsync(roles);
      await DeleteRoles(user);
    }

    private async Task SaveRoles(SocketGuildUser user, IEnumerable<SocketRole> roles)
    {
      foreach (var role in roles)
      {
        var sql = "INSERT INTO remember_roles(guild_id, user_id, role_id) VALUES($0, $1, $2)";
        await DatabaseService.NonQuery(sql, user.Guild.Id, user.Id, role.Id);
      }
    }

    private async Task<List<ulong>> LoadRoles(SocketGuildUser user)
    {
      var sql = "SELECT role_id FROM remember_roles WHERE guild_id = $0 AND user_id = $1";
      var roles = await DatabaseService.Query<ulong>(sql, user.Guild.Id, user.Id);
      return roles;
    }

    private async Task DeleteRoles(SocketGuildUser user)
    {
      var sql = "DELETE FROM remember_roles WHERE guild_id = $0 AND user_id = $1";
      await DatabaseService.NonQuery(sql, user.Guild.Id, user.Id);
    }

    private async Task CreateRememberRolesTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS remember_roles(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          user_id BIGINT UNSIGNED NOT NULL,
          role_id BIGINT UNSIGNED NOT NULL
        )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
