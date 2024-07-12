using Discord;
using Discord.WebSocket;
using Moe.Services;

namespace Moe.Commands;

public class UserinfoCommand : SlashCommandBase
{
  private readonly UserInfoService service;

  public UserinfoCommand(UserInfoService service) : base("userinfo")
  {
    Description = "Get the info of the given user";
    Options = new SlashCommandOptionBuilder()
      .AddOption("user", ApplicationCommandOptionType.User, "The user to get the information of", isRequired: false);
    this.service = service;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    await cmd.DeferAsync();

    var user = cmd.GetOption<SocketGuildUser>("user") ?? (SocketGuildUser)cmd.User;

    var firstJoined = await service.FirstJoined(user);
    var roles = "None";
    if (user.Roles.Count > 1)
    {
      roles = RolesToString(user.Roles);
    }

    var embed = new EmbedBuilder()
      .WithAuthor(user)
      .WithDescription($"[Avatar]({user.GetAvatarUrl(size: 1024)})")
      .AddField("Roles", $"{roles}", inline: true)
      .AddField("Created at", $"{user.CreatedAt:yyyy-MM-dd HH:mm}", inline: true)
      .AddField("First joined at", firstJoined?.ToString("yyyy-MM-dd HH:mm") ?? "Unkown", inline: true)
      .AddField("Last joined at", $"{user.JoinedAt:yyyy-MM-dd HH:mm}", inline: true)
      .WithColor(Colors.GetMainRoleColor(user))
      .WithFooter($"ID: {user.Id}");

    await cmd.FollowupAsync(embed: embed.Build());
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
