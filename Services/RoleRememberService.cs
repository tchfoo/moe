using Discord.WebSocket;

namespace Moe.Services;

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
    var guild = user.Guild;
    var availableRoles = guild.Roles.Select(x => x.Id);
    var roles = (await LoadRoles(user))
      .Where(x => availableRoles.Contains(x))
      .Select(x => guild.GetRole(x))
      .ToList();

    var highestBotRole = guild.CurrentUser.Roles.OrderByDescending(x => x.Position).First();
    foreach (var role in roles.ToList())
    {
      if (role.Position > highestBotRole.Position)
      {
        await LogService.Instance.LogToDiscord(guild, $"{Emotes.ErrorEmote} Role {role.Mention} is in a higher position than my role ({highestBotRole.Mention}), therefore I can't apply this role to {user.Mention} for joining back");
        roles.Remove(role);
      }
    }

    await LogService.LogToFileAndConsole(
      $"User rejoined, adding roles {string.Join(", ", roles)} to {user}", user.Guild);

    await user.AddRolesAsync(roles);
    await DeleteRoles(user);
  }

  private async Task SaveRoles(SocketGuildUser user, IEnumerable<SocketRole> roles)
  {
    var guild = user.Guild;
    foreach (var role in roles)
    {
      var highestBotRole = guild.CurrentUser.Roles.OrderByDescending(x => x.Position).First();
      if (role.Position > highestBotRole.Position)
      {
        await LogService.Instance.LogToDiscord(guild, $"{Emotes.ErrorEmote} Role {role.Mention} is in a higher position than my role ({highestBotRole.Mention}), therefore I won't be able to apply this role to {user.Mention} when they join back");
      }

      var sql = "INSERT INTO remember_roles(guild_id, user_id, role_id) VALUES($0, $1, $2)";
      await DatabaseService.NonQuery(sql, user.Guild.Id, user.Id, role.Id);
    }
  }

  private async Task<List<ulong>> LoadRoles(SocketGuildUser user)
  {
    var sql = "SELECT role_id FROM remember_roles WHERE guild_id = $0 AND user_id = $1";
    return await DatabaseService.Query<ulong>(sql, user.Guild.Id, user.Id);
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
