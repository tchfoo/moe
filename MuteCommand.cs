using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace TNTBot
{
  public class MuteCommand : SlashCommandBase
  {
    private static readonly TimeSpan DefaultMuteDuration = TimeSpan.FromMinutes(30);
    public override string CommandName { get => "mute"; }

    public override async Task Register()
    {
      await RegisterSlashCommand(new SlashCommandBuilder()
        .WithName("mute")
        .WithDescription("Mute a user.")
        .AddOption("user", ApplicationCommandOptionType.User, "The user to mute.", isRequired: true)
        .AddOption("time", ApplicationCommandOptionType.String, "Duration of the mute (eg. 1h 30m)", isRequired: false)
        .AddOption("reason", ApplicationCommandOptionType.String, "Reason for the mute.", isRequired: false));

      await Services.ExecuteSqlNonQuery(
        @"CREATE TABLE IF NOT EXISTS mutes(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          user_id INTEGER NOT NULL,
          expire_at TEXT NOT NULL,
          reason TEXT
        )");

      var mutes = await Services.ExecuteSqlQuery("SELECT guild_id, user_id, expire_at FROM mutes");
      foreach (var mute in mutes)
      {
        var guildId = ulong.Parse(mute[0]);
        var userId = ulong.Parse(mute[1]);
        var expireAt = DateTime.Parse(mute[2]);
        var duration = expireAt - DateTime.Now;

        var guild = Services.Client.GetGuild(guildId);
        var user = guild.GetUser(userId);

        var muteRoleName = "muted-by-tntbot";
        if (!guild.Roles.Any(x => x.Name == muteRoleName))
        {
          var mutedPerms = new GuildPermissions(sendMessages: false);
          await guild.CreateRoleAsync(muteRoleName, mutedPerms, Color.DarkRed, false, null);
        }
        var mutedRole = guild.Roles.First(x => x.Name == muteRoleName);

        StartUnmuteTimer(duration, mutedRole, user, guildId, userId);
      }
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var options = cmd.Data.Options;
      var user = (SocketGuildUser)options.First(x => x.Name == "user").Value;
      TimeSpan duration;
      if (options.Any(x => x.Name == "time"))
      {
        duration = ParseDuration((string)options.First(x => x.Name == "time"));
      }
      else
      {
        duration = DefaultMuteDuration;
      }
      var expireAt = DateTime.Now + duration;
      string? reason = null;
      if (options.Any(x => x.Name == "reason"))
      {
        reason = (string)options.First(x => x.Name == "reason").Value;
      }

      var guild = (cmd.Channel as SocketGuildChannel).Guild;

      var muteRoleName = "muted-by-tntbot";
      if (!guild.Roles.Any(x => x.Name == muteRoleName))
      {
        var mutedPerms = new GuildPermissions(sendMessages: false);
        await guild.CreateRoleAsync(muteRoleName, mutedPerms, Color.DarkRed, false, null);
      }
      var mutedRole = guild.Roles.First(x => x.Name == muteRoleName);

      var guildId = guild.Id;
      var userId = user.Id;

      var mutesCountSql = $"SELECT COUNT(*) FROM mutes WHERE guild_id = {guildId} AND user_id = {userId}";
      int mutesCount = int.Parse((await Services.ExecuteSqlQuery(mutesCountSql))[0][0]);
      if (mutesCount > 0)
      {
        await cmd.RespondAsync($"**{user}** is already muted.");
        return;
      }

      var muteSql = $@"
        INSERT INTO mutes(guild_id, user_id, expire_at, reason)
        VALUES({guildId}, {userId}, '{expireAt}', '{reason ?? "NULL"}')
      ";
      await Services.ExecuteSqlNonQuery(muteSql);

      await user.AddRoleAsync(mutedRole);

      StartUnmuteTimer(duration, mutedRole, user, guildId, userId);

      await cmd.RespondAsync($"Muted **{user}** for {duration}. Reason: {reason ?? "unspecified"}.");
    }

    public static Task StartUnmuteTimer(TimeSpan duration, SocketRole mutedRole, SocketGuildUser user, ulong guildId, ulong userId)
    {
      return Task.Run(async () =>
      {
        if (duration.TotalMilliseconds > 0)
        {
          await Task.Delay((int)duration.TotalMilliseconds);
        }
        var deleteMuteSql = $"DELETE FROM mutes WHERE guild_id = {guildId} AND user_id = {userId}";
        await user.RemoveRoleAsync(mutedRole);
        await Services.ExecuteSqlNonQuery(deleteMuteSql);
      });
    }

    private TimeSpan ParseDuration(string s)
    {
      var days = ParsePostfixedNumber(s, "d");
      var hours = ParsePostfixedNumber(s, "h");
      var minutes = ParsePostfixedNumber(s, "m");
      var seconds = ParsePostfixedNumber(s, "s");
      return new TimeSpan(days, hours, minutes, seconds);
    }

    private int ParsePostfixedNumber(string text, string postfix)
    {
      var match = Regex.Match(text, $@"\d+\s*{postfix}");
      if (!match.Success)
      {
        return 0;
      }

      var withoutPostfix = match.Value.Replace(postfix, string.Empty);
      return int.Parse(withoutPostfix);
    }
  }
}
