using Discord;
using Discord.WebSocket;
using Moe.Models;

namespace Moe.Services;

public class CustomRoleService
{
  private readonly SettingsService settingsService;

  public CustomRoleService(SettingsService settingsService)
  {
    CreateRolesTable().Wait();
    this.settingsService = settingsService;
  }

  public bool IsAuthorized(SocketGuildUser user, ModrankLevel requiredLevel, out string? error)
  {
    return settingsService.IsAuthorized(user, requiredLevel, out error);
  }

  public async Task<bool> HasRole(SocketGuild guild, string name)
  {
    await RemoveBrokenRoles(guild);
    var sql = "SELECT COUNT(*) FROM custom_roles WHERE guild_id = $0 AND name = $1";
    var count = await DatabaseService.QueryFirst<int>(sql, guild.Id, name);
    return count > 0;
  }

  public async Task<List<CustomRole>> GetRoles(SocketGuild guild)
  {
    var sql = "SELECT name, description, role_id FROM custom_roles WHERE guild_id = $0";
    var roles = await DatabaseService.Query<string, string, ulong>(sql, guild.Id);
    return roles.ConvertAll(x => new CustomRole(x.Item1!, x.Item2, guild.GetRole(x.Item3!)));
  }

  public async Task AddRole(SocketGuild guild, string name, string? description, SocketRole role)
  {
    await LogService.LogToFileAndConsole(
      $"Adding custom role {name}, description: {description}, discord role: {role}", guild);

    var sql = "INSERT INTO custom_roles(guild_id, name, description, role_id) VALUES($0, $1, $2, $3)";
    await DatabaseService.NonQuery(sql, guild.Id, name, description, role.Id);
  }

  public async Task RemoveRole(SocketGuild guild, string name)
  {
    await LogService.LogToFileAndConsole(
      $"Removing custom role {name}", guild);

    var sql = "DELETE FROM custom_roles WHERE guild_id = $0 AND name = $1";
    await DatabaseService.NonQuery(sql, guild.Id, name);
  }

  public async Task<List<CustomRole>> GetSubscribedRoles(SocketGuildUser user)
  {
    return (await GetRoles(user.Guild))
      .Where(x => user.Roles.Contains(x.DiscordRole))
      .ToList();
  }

  public async Task SyncRoleSubscriptions(SocketGuildUser user, IEnumerable<CustomRole> oldRoles, IEnumerable<CustomRole> newRoles)
  {
    var oldRolesSet = new HashSet<IRole>(oldRoles.Select(x => x.DiscordRole));
    var newRolesSet = new HashSet<IRole>(newRoles.Select(x => x.DiscordRole));

    var toSubscribe = newRolesSet.Except(oldRolesSet);
    var toUnsubscribe = oldRolesSet.Except(newRolesSet);

    var toSubscribeLog = string.Join(",", toSubscribe.Select(x => x.Name));
    var toUnsubscribeLog = string.Join(",", toUnsubscribe.Select(x => x.Name));
    await LogService.LogToFileAndConsole(
      $"Syncing role subscriptions for {user}: subscribing to {toSubscribeLog}; unsubscribing from: {toUnsubscribeLog}", user.Guild);

    await user.AddRolesAsync(toSubscribe);
    await user.RemoveRolesAsync(toUnsubscribe);
  }

  private async Task RemoveBrokenRoles(SocketGuild guild)
  {
    var brokenRoles = (await GetRoles(guild)).Where(x => x.DiscordRole == null);
    foreach (var role in brokenRoles)
    {
      await LogService.LogToFileAndConsole(
        $"Removing broken custom role {role.Name}", guild);
      await RemoveRole(guild, role.Name);
    }
  }

  private async Task CreateRolesTable()
  {
    var sql = @"
      CREATE TABLE IF NOT EXISTS custom_roles(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        guild_id INTEGER NOT NULL,
        name TEXT NOT NULL,
        description TEXT,
        role_id INTEGER NOT NULL
      )";
    await DatabaseService.NonQuery(sql);
  }
}
