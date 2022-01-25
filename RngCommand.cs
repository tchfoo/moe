using Discord;
using Discord.WebSocket;

namespace TNTBot
{
  public class RngCommand : SlashCommandBase
  {
    public override string CommandName { get => "rng"; }

    public override async Task Register()
    {
      await RegisterSlashCommand(new SlashCommandBuilder()
        .WithName("rng")
        .WithDescription("Generates a random number.")
        .AddOption("max", ApplicationCommandOptionType.Integer, "The maximum number the bot generates.", isRequired: true)
        .AddOption("min", ApplicationCommandOptionType.Integer, "The minimum number the bot generates. Defaults to 1.", isRequired: false));
    }
    public override async Task Handle(SocketSlashCommand cmd)
    {
      (var rngMin, var rngMax) = ParseOptions(cmd);

      if (await IsFakeRngNumberAvailable())
      {
        await PrintFakeRng(cmd);
      }
      else
      {
        await PrintRealRng(rngMin, rngMax, cmd);
      }
    }

    private (long, long) ParseOptions(SocketSlashCommand cmd)
    {
      long rngMin = 0, rngMax = 0;
      foreach (var cmdOption in cmd.Data.Options)
      {
        switch (cmdOption.Name)
        {
          case "min":
            rngMin = (long)cmdOption.Value;
            break;
          case "max":
            rngMax = (long)cmdOption.Value;
            break;
        }
      }
      return (rngMin, rngMax);
    }

    private async Task<bool> IsFakeRngNumberAvailable()
    {
      var checkCountQuery = await Services.ExecuteSqlQuery("SELECT COUNT(id) FROM rngnums;");
      int checkCount = int.Parse(checkCountQuery[0][0]);
      return checkCount > 0;
    }

    private async Task PrintFakeRng(SocketSlashCommand cmd)
    {
      var selectedNumQuery = await Services.ExecuteSqlQuery("SELECT * FROM rngnums LIMIT(1);");
      int selectedNum = int.Parse(selectedNumQuery[0][1]);
      await cmd.RespondAsync(selectedNum.ToString());

      await Services.ExecuteSqlNonQuery("DELETE FROM rngnums WHERE id = (SELECT id FROM rngnums LIMIT(1));");
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
