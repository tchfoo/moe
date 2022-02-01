using Discord.WebSocket;
using TNTBot.Models;

namespace TNTBot.Services
{
  public class CustomRoleService
  {
    public CustomRoleService()
    {
      CreateRolesTable().Wait();
    }

    public async Task<bool> HasRoles(SocketGuild guild)
    {
      var sql = "SELECT COUNT(*) FROM custom_roles WHERE guild_id = $0";
      var count = await DatabaseService.QueryFirst<int>(sql, guild.Id);
      return count > 0;
    }

    public async Task<bool> HasRole(SocketGuild guild, string name)
    {
      var sql = "SELECT COUNT(*) FROM custom_roles WHERE guild_id = $0 AND name = $1";
      var count = await DatabaseService.QueryFirst<int>(sql, guild.Id, name);
      return count > 0;
    }

    public async Task<List<CustomRole>> GetRoles(SocketGuild guild)
    {
      var sql = "SELECT name, role_id FROM custom_roles WHERE guild_id = $0";
      var result = await DatabaseService.Query<string, ulong>(sql, guild.Id);
      return result.ConvertAll(x => new CustomRole(x.Item1!, guild.GetRole(x.Item2!)));
    }

    public async Task<CustomRole?> GetRole(SocketGuild guild, string name)
    {
      var sql = "SELECT role_id FROM custom_roles WHERE guild_id = $0 AND name = $1";
      var result = await DatabaseService.Query<ulong>(sql, guild.Id, name);
      if (result.Count == 0)
      {
        return null;
      }

      return new CustomRole(name, guild.GetRole(result[0]));
    }

    public async Task AddRole(SocketGuild guild, string name, SocketRole role)
    {
      var sql = "INSERT INTO custom_roles(guild_id, name, role_id) VALUES($0, $1, $2)";
      await DatabaseService.NonQuery(sql, guild.Id, name, role.Id);
    }

    public async Task RemoveRole(SocketGuild guild, string name)
    {
      var sql = "DELETE FROM custom_roles WHERE guild_id = $0 AND name = $1";
      await DatabaseService.NonQuery(sql, guild.Id, name);
    }

    public async Task<bool> IsSubscribedToRole(SocketGuildUser user, string name)
    {
      var customRole = (await GetRole(user.Guild, name))!;
      return user.Roles.Any(x => x.Id == customRole.DiscordRole.Id);
    }

    public async Task SubscribeToRole(SocketGuildUser user, string name)
    {
      var customRole = (await GetRole(user.Guild, name))!;
      await user.AddRoleAsync(customRole.DiscordRole);
    }

    public async Task UnsubscribeFromRole(SocketGuildUser user, string name)
    {
      var customRole = (await GetRole(user.Guild, name))!;
      await user.RemoveRoleAsync(customRole.DiscordRole);
    }

    private async Task CreateRolesTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS custom_roles(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          name TEXT NOT NULL,
          role_id INTEGER NOT NULL
        )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
