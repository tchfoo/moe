using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class RngCommand : SlashCommandBase
  {
    public RngCommand() : base("rng", "Generates a random number.")
    {
      Options = new SlashCommandOptionBuilder()
        .AddOption("min", ApplicationCommandOptionType.Integer, "The maximum number the bot generates.", isRequired: true)
        .AddOption("max", ApplicationCommandOptionType.Integer, "The minimum number the bot generates. Defaults to 1.", isRequired: true);
    }

    public override async Task OnRegister()
    {
      await DatabaseService.NonQuery("CREATE TABLE IF NOT EXISTS rngnums(id INTEGER PRIMARY KEY AUTOINCREMENT, num INTEGER)");
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      long rngMin = cmd.GetOption<long>("min", 0);
      long rngMax = cmd.GetOption<long>("max", 0);

      if (await IsFakeRngNumberAvailable())
      {
        await PrintFakeRng(cmd);
      }
      else
      {
        await PrintRealRng(rngMin, rngMax, cmd);
      }
    }

    private async Task<bool> IsFakeRngNumberAvailable()
    {
      var checkCount = await DatabaseService.QueryFirst<int>("SELECT COUNT(id) FROM rngnums;");
      return checkCount > 0;
    }

    private async Task PrintFakeRng(SocketSlashCommand cmd)
    {
      var selectedNum = await DatabaseService.QueryFirst<int>("SELECT num FROM rngnums LIMIT(1);");
      await cmd.RespondAsync(selectedNum.ToString());

      await DatabaseService.NonQuery("DELETE FROM rngnums WHERE id = (SELECT id FROM rngnums LIMIT(1));");
    }

    private async Task PrintRealRng(long rngMin, long rngMax, SocketSlashCommand cmd)
    {
      var xEmoji = "\u274C";
      if (rngMin > rngMax)
      {
        await cmd.RespondAsync($"{xEmoji} A minimum szám nem lehet nagyobb, mint a maximum!");
        return;
      }
      else if (rngMin == rngMax)
      {
        await cmd.RespondAsync($"{xEmoji} A két szám nem lehet ugyan az!");
        return;
      }

      long rngNum = Random.Shared.NextInt64(rngMin, rngMax);
      await cmd.RespondAsync(rngNum.ToString());
    }
  }
}
