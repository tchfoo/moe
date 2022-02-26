using Discord;
using Discord.WebSocket;

namespace TNTBot.Commands
{
  public class UserinfoCommand : SlashCommandBase
  {
    public UserinfoCommand() : base("userinfo")
    {
      Description = "Get the info of the given user";
      Options = new SlashCommandOptionBuilder()
        .AddOption("user", ApplicationCommandOptionType.User, "The user to get the information of", isRequired: false);
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = cmd.GetOption<SocketGuildUser>("user") ?? (SocketGuildUser)cmd.User;

      var roles = "No roles";
      if (user.Roles.Count > 0)
      {
        roles = RolesToString(user.Roles);
      }

      var embed = new EmbedBuilder()
        .WithAuthor(user)
        .WithDescription($"[Avatar]({user.GetAvatarUrl()})")
        .AddField("Roles", $"{roles}", inline: true)
        .AddField("Created at", $"{user.CreatedAt: yyyy-MM-dd HH:mm}", inline: true)
        .AddField("First joined at", "FirstJoin", inline: true)
        .AddField("Last joined at", $"{user.JoinedAt: yyyy-MM-dd HH:mm}", inline: true)
        .WithColor(user.Roles.Last().Color)
        .WithFooter($"ID: {user.Id}");

      await cmd.RespondAsync(embed: embed.Build());
    }

    private string RolesToString(IReadOnlyCollection<SocketRole> allRoles)
    {
      var roles = allRoles.Where(x => !x.IsEveryone).ToList();
      string rolesOut = "";
      if (roles.Count > 0)
      {
        foreach (var role in roles)
        {
          rolesOut += $" {role.Mention}";
        }
      }
      return rolesOut;
    }
  }
}
