using Discord;
using Discord.WebSocket;
using TNTBot.Models;

namespace TNTBot.Services
{
  public class SnapshotService
  {
    private readonly SettingsService settingsService;

    public SnapshotService(SettingsService settingsService)
    {
      CreateSnapshotsTable().Wait();
      CreateSnapshotChannelsTable().Wait();
      CreateSnapshotRolesTable().Wait();
      this.settingsService = settingsService;
    }

    public bool IsAuthorized(SocketGuildUser user, ModrankLevel requiredLevel, out string? error)
    {
      return settingsService.IsAuthorized(user, requiredLevel, out error);
    }

    public async Task<bool> HasSnapshots(SocketGuild guild)
    {
      var sql = "SELECT COUNT(*) FROM snapshots WHERE guild_id = $0";
      var count = await DatabaseService.QueryFirst<int>(sql, guild.Id);
      return count > 0;
    }

    public async Task<bool> HasSnapshot(SocketGuild guild, string name)
    {
      var sql = "SELECT COUNT(*) FROM snapshots WHERE guild_id = $0 AND name = $1";
      var count = await DatabaseService.QueryFirst<int>(sql, guild.Id, name);
      return count > 0;
    }

    public async Task<List<string>> ListSnapshots(SocketGuild guild)
    {
      var sql = "SELECT name FROM snapshots WHERE guild_id = $0";
      var snapshots = await DatabaseService.Query<string>(sql, guild.Id);
      return snapshots.ConvertAll(x => x!);
    }

    public async Task<SnapshotModel> GetSnapshot(SocketGuild guild, string name)
    {
      var id = await GetSnapshotId(guild, name);

      var sql = "SELECT guild_name, guild_icon FROM snapshots WHERE id = $0";
      var snapshot = await DatabaseService.Query<string, string>(sql, id);
      var s = new SnapshotModel
      {
        Id = id,
        Guild = guild,
        Name = name,
        GuildName = snapshot[0].Item1!,
      };
      if (!string.IsNullOrEmpty(snapshot[0].Item2))
      {
        s.GuildIcon = new Image(new MemoryStream(Convert.FromBase64String(snapshot[0].Item2!)));
      }

      sql = "SELECT channel_id, channel_name FROM snapshot_channels WHERE snapshot_id = $0";
      var channels = await DatabaseService.Query<ulong, string>(sql, id);
      s.Channels = channels.ToDictionary(k => k.Item1, v => v.Item2!);

      sql = "SELECT role_id, role_name FROM snapshot_roles WHERE snapshot_id = $0";
      var roles = await DatabaseService.Query<ulong, string>(sql, id);
      s.Roles = roles.ToDictionary(k => k.Item1, v => v.Item2!);

      return s;
    }

    public async Task AddSnapshot(SocketGuild guild, string name, bool voicenames, bool rolenames, bool servericon)
    {
      var s = new SnapshotModel
      {
        Guild = guild,
        Name = name,
        GuildName = guild.Name,
        Channels = guild.TextChannels
          .Where(CanBotManageChannel)
          .Cast<SocketGuildChannel>()
          .ToDictionary(k => k.Id, v => v.Name),
      };

      if (voicenames)
      {
        var voiceChannels = guild.VoiceChannels
          .Where(CanBotManageChannel)
          .Cast<SocketGuildChannel>();
        foreach (var channel in voiceChannels)
        {
          s.Channels.Add(channel.Id, channel.Name);
        }
      }
      if (rolenames)
      {
        s.Roles = guild.Roles
          .Where(CanBotManageRole)
          .ToDictionary(x => x.Id, x => x.Name);
      }
      if (servericon)
      {
        var client = new HttpClient();
        var stream = await client.GetStreamAsync(guild.IconUrl);
        s.GuildIcon = new Image(stream);
      }

      await AddSnapshot(s);
    }

    public async Task RemoveSnapshot(SocketGuild guild, string name)
    {
      await LogService.LogToFileAndConsole(
        $"Removing snapshot {name}", guild);

      var id = await GetSnapshotId(guild, name);

      var sql = "DELETE FROM snapshot_channels WHERE snapshot_id = $0";
      await DatabaseService.NonQuery(sql, id);

      sql = "DELETE FROM snapshot_roles WHERE snapshot_id = $0";
      await DatabaseService.NonQuery(sql, id);

      sql = "DELETE FROM snapshots WHERE id = $0";
      await DatabaseService.NonQuery(sql, id);
    }

    public async Task<string> RestoreSnapshot(SocketGuild guild, string name)
    {
      var errors = string.Empty;
      var s = await GetSnapshot(guild, name);

      await RestoreGuildInfo(guild, s);

      errors += await RestoreChannels(guild, s.Channels);

      if (s.Roles is not null)
      {
        errors += await RestoreRoles(guild, s.Roles);
      }

      return errors;
    }

    public EmbedBuilder GetSnapshotDump(SnapshotModel s)
    {
      var embed = new EmbedBuilder()
        .WithTitle(GetGuildNameDump(s))
        .WithColor(Colors.Grey);

      var channels = GetChannelsDump(s);
      if (channels != string.Empty)
      {
        embed.AddField("Channels", channels, inline: true);
      }

      var roles = GetRolesDump(s);
      if (roles != string.Empty)
      {
        embed.AddField("Roles", roles, inline: true);
      }

      if (s.GuildIcon != null)
      {
        embed.WithFooter("Attached server icon to message.");
      }

      return embed;
    }

    private string GetChannelsDump(SnapshotModel s)
    {
      var result = string.Empty;

      if (s.Channels.Count > 0)
      {
        foreach (var c in s.Channels)
        {
          var channel = s.Guild.Channels
            .FirstOrDefault(x => x.Id == c.Key);
          if (channel is null)
          {
            result += $"~~{c.Value}~~ (deleted, id: {c.Key}) ";
          }
          else
          {
            var mention = (channel as IMentionable)!.Mention;
            var channelName = channel.Name == c.Value ? mention : $"**{c.Value} (currently {mention})**";
            result += $"{channelName} ";
          }
        }
      }

      return result;
    }

    private string GetRolesDump(SnapshotModel s)
    {
      var result = string.Empty;

      if (s.Roles?.Any() == true)
      {
        foreach (var r in s.Roles)
        {
          var role = s.Guild.Roles.FirstOrDefault(x => x.Id == r.Key);
          if (role is null)
          {
            result += $"~~{r.Value}~~ (deleted, id: {r.Key}) ";
          }
          else
          {
            var mention = role.Mention;
            var roleName = role.Name == r.Value ? mention : $"**{r.Value} (currently {mention})**";
            result += $"{roleName} ";
          }
        }
      }

      return result;
    }

    private string GetGuildNameDump(SnapshotModel s)
    {
      var guildName = s.GuildName == s.Guild.Name ? s.GuildName : $"**{s.GuildName}** (currently {s.Guild.Name})**";

      return guildName;
    }

    private async Task AddSnapshot(SnapshotModel s)
    {
      var channelsString = string.Join(",", s.Channels);
      var rolesString = s.Roles is null ? string.Empty : string.Join(",", s.Roles);
      await LogService.LogToFileAndConsole(
        $"Adding snapshot {s.Name} with parameters guild: {s.Guild}, guild icon included: {s.GuildIcon.HasValue}, channels: ({channelsString}), roles: {rolesString})", s.Guild);

      string guildIcon = string.Empty;
      if (s.GuildIcon is not null)
      {
        var ms = new MemoryStream();
        await s.GuildIcon.Value.Stream.CopyToAsync(ms);
        guildIcon = Convert.ToBase64String(ms.ToArray());
      }
      var sql = "INSERT INTO snapshots(guild_id, name, guild_name, guild_icon) VALUES ($0, $1, $2, $3)";
      await DatabaseService.NonQuery(sql, s.Guild.Id, s.Name, s.GuildName, guildIcon);

      var id = await GetSnapshotId(s.Guild, s.Name);

      if (s.Channels.Count > 0)
      {
        var values = string.Join(",", s.Channels.Select(x => $"({id}, {x.Key}, '{x.Value}')"));
        sql = $"INSERT INTO snapshot_channels(snapshot_id, channel_id, channel_name) VALUES {values}";
        await DatabaseService.NonQuery(sql);
      }

      if (s.Roles is not null)
      {
        var values = string.Join(",", s.Roles.Select(x => $"({id}, {x.Key}, '{x.Value}')"));
        sql = $"INSERT INTO snapshot_roles(snapshot_id, role_id, role_name) VALUES {values}";
        await DatabaseService.NonQuery(sql);
      }
    }

    private async Task<int> GetSnapshotId(SocketGuild guild, string name)
    {
      var sql = "SELECT id FROM snapshots WHERE guild_id = $0 AND name = $1";
      return await DatabaseService.QueryFirst<int>(sql, guild.Id, name);
    }

    private async Task RestoreGuildInfo(SocketGuild guild, SnapshotModel s)
    {
      var guildIconUpdate = s.GuildIcon is not null;
      var guildNameUpdate = s.GuildName != guild.Name;
      if (guildIconUpdate || guildNameUpdate)
      {
        await guild.ModifyAsync(async x =>
        {
          if (guildIconUpdate)
          {
            await LogService.LogToFileAndConsole(
              "Restoring guild icon", guild);
            x.Icon = s.GuildIcon;
          }

          if (guildNameUpdate)
          {
            await LogService.LogToFileAndConsole(
              $"Restoring guild name from {guild.Name} to {s.GuildName}", guild);
            x.Name = s.GuildName;
          }
        });
      }
    }

    private async Task<string> RestoreChannels(SocketGuild guild, Dictionary<ulong, string> sChannels)
    {
      var errors = string.Empty;

      var permissionFails = new List<string>();
      var toModify = guild.Channels
        .Where(x => sChannels.ContainsKey(x.Id)
          && x.Name != sChannels[x.Id]);
      foreach (var channel in toModify)
      {
        var restoreName = sChannels[channel.Id];
        if (CanBotManageChannel(channel))
        {
          await LogService.LogToFileAndConsole(
            $"Restoring channel name from {channel.Name} to {restoreName}", guild);
          await channel.ModifyAsync(x => x.Name = restoreName);
        }
        else
        {
          permissionFails.Add($" - {(channel as IMentionable)!.Mention} to {restoreName}");
        }
      }

      if (permissionFails.Count > 0)
      {
        errors += $"Failed to restore the following channels' names due to lack permissions:\n{string.Join("\n", permissionFails)}\n";
      }

      var deleteFails = sChannels
        .Where(x => !guild.Channels.Any(y => x.Key == y.Id))
        .Select(x => $" - {x.Value} (id: {x.Key})");
      if (deleteFails.Any())
      {
        errors += $"Failed to restore the following channels' names as they no longer exist:\n{string.Join("\n", deleteFails)}\n";
      }

      return errors;
    }

    private async Task<string> RestoreRoles(SocketGuild guild, Dictionary<ulong, string> sRoles)
    {
      var errors = string.Empty;

      var permissionFails = new List<string>();
      var rolesToModify = guild.Roles
      .Where(x => sRoles?.ContainsKey(x.Id) == true
        && x.Name != sRoles[x.Id]);
      foreach (var role in rolesToModify)
      {
        var restoreName = sRoles[role.Id];
        if (CanBotManageRole(role))
        {
          await LogService.LogToFileAndConsole(
            $"Restoring role name from {role.Name} to {restoreName}", guild);
          await role.ModifyAsync(x => x.Name = restoreName);
        }
        else
        {
          permissionFails.Add($" - {(role as IMentionable)!.Mention} to {restoreName}");
        }
      }

      if (permissionFails.Count > 0)
      {
        errors += $"Failed to restore the following roles' names due to lack permissions:\n{string.Join("\n", permissionFails)}\n";
      }

      var deleteFails = sRoles
        .Where(x => !guild.Roles.Any(y => x.Key == y.Id))
        .Select(x => $" - {x.Value} (id: {x.Key})");
      if (deleteFails.Any())
      {
        errors += $"Failed to restore the following roles' names as they no longer exist:\n{string.Join("\n", deleteFails)}\n";
      }

      return errors;
    }

    private bool CanBotManageChannel(SocketGuildChannel channel)
    {
      var guild = channel.Guild;
      var overwrites = channel.PermissionOverwrites;
      var bot = guild.CurrentUser;

      var userOverwrites = overwrites
        .Where(x => x.TargetType == PermissionTarget.User
          && x.TargetId == bot.Id);
      var roleOverwrites = overwrites
        .Where(x => x.TargetType == PermissionTarget.Role
          && bot.Roles.Select(x => x.Id).Contains(x.TargetId));
      var botOverwrites = userOverwrites.Concat(roleOverwrites);

      return !botOverwrites.Any(x => x.Permissions.ManageChannel == PermValue.Deny);
    }

    private bool CanBotManageRole(SocketRole role)
    {
      var guild = role.Guild;
      var bot = guild.CurrentUser;

      var highestBotRole = bot.Roles.OrderByDescending(x => x.Position).First();

      return role.Position < highestBotRole.Position;
    }

    private async Task CreateSnapshotsTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS snapshots(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          name TEXT NOT NULL,
          guild_name TEXT NOT NULL,
          guild_icon BLOB
        )";
      await DatabaseService.NonQuery(sql);
    }

    private async Task CreateSnapshotChannelsTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS snapshot_channels(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          snapshot_id INTEGER NOT NULL,
          channel_id INTEGER NOT NULL,
          channel_name TEXT NOT NULL,
          FOREIGN KEY(snapshot_id) REFERENCES snapshots(id)
        )";
      await DatabaseService.NonQuery(sql);
    }

    private async Task CreateSnapshotRolesTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS snapshot_roles(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          snapshot_id INTEGER NOT NULL,
          role_id INTEGER NOT NULL,
          role_name TEXT NOT NULL,
          FOREIGN KEY(snapshot_id) REFERENCES snapshots(id)
        )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
