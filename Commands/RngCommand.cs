using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class RngCommand : SlashCommandBase
  {
    public RngCommand() : base("rng")
    {
      Description = "Generates a random number";
      Options = new SlashCommandOptionBuilder()
        .AddOption("min", ApplicationCommandOptionType.Integer, "The maximum number the bot generates", isRequired: true)
        .AddOption("max", ApplicationCommandOptionType.Integer, "The minimum number the bot generates. Defaults to 1", isRequired: true);
    }

    //DISCLAIMER: This command contains an unfair advantage, where you can define your own RNG pool. The bot authors are not responsible for the use/abuse of this feature, and never would use (or used) it themselves. This feature is only intended for educational purposes only.
    public override async Task OnRegister()
    {
      await DatabaseService.NonQuery(@"
        CREATE TABLE IF NOT EXISTS rngnums(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          num INTEGER
        )");
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

    public async Task<bool> HandleDM(SocketMessage msg, string name, List<string> args)
    {
      if (msg.Channel.GetChannelType() != ChannelType.DM)
      {
        return false;
      }

      var owners = ConfigService.Config.Owners;
      var yaha = ConfigService.Config.Yaha;
      var author = msg.Author.Id;
      if (!(owners.Contains(author) || author == yaha))
      {
        return false;
      }

      switch (name)
      {
        case "setrng":
          await SetRng(msg, args);
          return true;
        case "clearrng":
          await ClearRng(msg);
          return true;
      }

      return false;
    }

    private async Task SetRng(SocketMessage msg, List<string> args)
    {
      try
      {
        List<string> numsList = args
          .Select(int.Parse)
          .Select(x => $"({x})")
          .ToList();
        string numsSql = string.Join(',', numsList);
        var addSql = $"INSERT INTO rngnums (num) VALUES {numsSql}";
        await DatabaseService.NonQuery(addSql);
        await msg.Channel.SendMessageAsync(numsSql);
      }
      catch (FormatException)
      {
        await msg.Channel.SendMessageAsync("Nem egész számokat adtál meg! Helyes szintaktika: `!setrng 10 20 30`");
      }
    }

    private async Task ClearRng(SocketMessage msg)
    {
      await DatabaseService.NonQuery("DELETE FROM rngnums");
      await msg.Channel.SendMessageAsync("Rng számok sikeresen törölve");
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
