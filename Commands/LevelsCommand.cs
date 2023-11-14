using Discord;
using Discord.WebSocket;
using Moe.Services;

namespace Moe.Commands;

public class LevelsCommand : SlashCommandBase
{
  private readonly LevelService service;

  public LevelsCommand(LevelService service) : base("levels")
  {
    Description = "Get the level leaderboard";
    this.service = service;
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    var guild = ((SocketGuildChannel)cmd.Channel).Guild;

    var leaderboard = await service.GetLeaderboard(guild);

    var levels = leaderboard.Select((x, i) =>
      $"**{i + 1}.** {x.User.Mention} • Level: {x.LevelNumber} • XP: {x.TotalXP}");
    var p = new PaginatableEmbedBuilder<string>
      (10, levels, items =>
        new EmbedBuilder()
          .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
          .WithDescription(string.Join('\n', items))
          .WithColor(Colors.Blurple)
      );

    await cmd.RespondAsync(embed: p.Embed, components: p.Components);
  }
}
