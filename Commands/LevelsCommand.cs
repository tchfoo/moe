using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
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
      var response = string.Empty;
      for (int i = 0; i < leaderboard.Count; i++)
      {
        var level = leaderboard[i];
        var rank = i + 1;
        response += $"#{rank} - **{level.User}** with {level.LevelNumber} levels which is {level.TotalXP} XP\n";
      }

      await cmd.RespondAsync(response);
    }
  }
}
