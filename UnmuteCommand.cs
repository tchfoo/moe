using System.Timers;
using Discord;
using Discord.WebSocket;

namespace TNTBot
{
  public class UnmuteCommand : SlashCommandBase
  {
    public override string CommandName { get => "unmute"; }

    public override async Task Register()
    {
      await RegisterSlashCommand(new SlashCommandBuilder()
        .WithName("unmute")
        .WithDescription("Unmute a user")
        .AddOption("user", ApplicationCommandOptionType.User, "The user to unmute.", isRequired: true));
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = (SocketGuildUser)cmd.Data.Options.First(x => x.Name == "user").Value;

      var guild = (cmd.Channel as SocketGuildChannel).Guild;
      var guildId = guild.Id;
      var userId = user.Id;

      var mutesCountSql = $"SELECT COUNT(*) FROM mutes WHERE guild_id = {guildId} AND user_id = {userId}";
      int mutesCount = int.Parse((await Services.ExecuteSqlQuery(mutesCountSql))[0][0]);
      if (mutesCount == 0)
      {
        await cmd.RespondAsync($"**{user}** was not muted.");
        return;
      }

      var deleteMuteSql = $"DELETE FROM mutes WHERE guild_id = {guildId} AND user_id = {userId}";
      await Services.ExecuteSqlNonQuery(deleteMuteSql);

      var muteRoleName = "muted-by-tntbot";
      if (!guild.Roles.Any(x => x.Name == muteRoleName))
      {
        var mutedPerms = new GuildPermissions(sendMessages: false);
        await guild.CreateRoleAsync(muteRoleName, mutedPerms, Color.DarkRed, false, null);
      }
      var mutedRole = guild.Roles.First(x => x.Name == muteRoleName);

      await user.RemoveRoleAsync(mutedRole);

      await cmd.RespondAsync($"Unmuted **{user}**.");
    }
  }
}
