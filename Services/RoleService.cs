using Discord.WebSocket;
using TNTBot.Models;

namespace TNTBot.Services
{
  public class RoleService
  {
    public RoleService()
    {
      CreateRolesTable().Wait();
    }

    public async Task<bool> HasRoles(SocketGuild guild)
    {
      var sql = "SELECT COUNT(*) FROM roles WHERE guild_id = $0";
      var count = await DatabaseService.QueryFirst<int>(sql, guild.Id);
      return count > 0;
    }

    public async Task<bool> HasRole(SocketGuild guild, string name)
    {
      var sql = "SELECT COUNT(*) FROM roles WHERE guild_id = $0 AND name = $1";
      var count = await DatabaseService.QueryFirst<int>(sql, guild.Id, name);
      return count > 0;
    }

    public async Task<List<Role>> GetRoles(SocketGuild guild)
    {
      var sql = "SELECT name, role_id FROM roles WHERE guild_id = $0";
      var result = await DatabaseService.Query<string, ulong>(sql, guild.Id);
      return result.ConvertAll(x => new Role(x.Item1!, guild.GetRole(x.Item2!)));
    }

    public async Task<Role?> GetRole(SocketGuild guild, string name)
    {
      var sql = "SELECT role_id FROM roles WHERE guild_id = $0 AND name = $1";
      var result = await DatabaseService.Query<ulong>(sql, guild.Id, name);
      if (result.Count == 0)
      {
        return null;
      }

      return new Role(name, guild.GetRole(result[0]));
    }

    public async Task AddRole(SocketGuild guild, string name, SocketRole role)
    {
      var sql = "INSERT INTO roles(guild_id, name, role_id) VALUES($0, $1, $2)";
      await DatabaseService.NonQuery(sql, guild.Id, name, role.Id);
    }

    public async Task RemoveRole(SocketGuild guild, string name)
    {
      var sql = "DELETE FROM roles WHERE guild_id = $0 AND name = $1";
      await DatabaseService.NonQuery(sql, guild.Id, name);
    }

    private async Task CreateRolesTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS roles(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          name TEXT NOT NULL,
          role_id INTEGER NOT NULL
        )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
