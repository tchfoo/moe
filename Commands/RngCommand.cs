using Discord;
using Discord.WebSocket;
using Moe.Models;
using Moe.Services;

namespace Moe.Commands;

public class RngCommand : SlashCommandBase
{
  private readonly RngService service;

  public RngCommand(RngService service) : base("rng")
  {
    Description = "Generates a random number";
    Options = new SlashCommandOptionBuilder()
      .AddOption("min", ApplicationCommandOptionType.Integer, "The minimum number the bot generates. Defaults to 1", isRequired: false)
      .AddOption("max", ApplicationCommandOptionType.Integer, "The maximum number the bot generates", isRequired: true);
    CreateRngnumsTable().Wait();
    this.service = service;
  }

  // DISCLAIMER: For those who can read this code: the creators of the bot are not responsible for the use/abuse of the questionable feature in this command and never would use (or used) it themselves. The feature is only intended for educational purposes only.
  private async Task CreateRngnumsTable()
  {
    await DatabaseService.NonQuery(@"
      CREATE TABLE IF NOT EXISTS rngnums(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        num INTEGER
      )");
  }

  public override async Task Handle(SocketSlashCommand cmd)
  {
    long rngMin = cmd.GetOption<long>("min", 1);
    long rngMax = cmd.GetOption<long>("max")!;

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
    var author = msg.Author;
    if (!service.IsAuthorizedDMSilent(author, ModrankLevel.Administrator))
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
      var nums = args
        .Select(int.Parse)
        .ToList();
      if (nums.Count == 0)
      {
        throw new FormatException();
      }

      var numsSql = string.Join(',', nums.Select(x => $"({x})"));
      var addSql = $"INSERT INTO rngnums (num) VALUES {numsSql}";
      await DatabaseService.NonQuery(addSql);

      var numsMessages = string.Join(", ", nums);
      await msg.Channel.SendMessageAsync($"{Emotes.SuccessEmote} Fake RNG numbers added: {numsMessages}");
    }
    catch (FormatException)
    {
      await msg.Channel.SendMessageAsync($"{Emotes.ErrorEmote} Invalid number(s). Example usage: `setrng 10 20 30`");
    }
  }

  private async Task ClearRng(SocketMessage msg)
  {
    await DatabaseService.NonQuery("DELETE FROM rngnums");
    await msg.Channel.SendMessageAsync($"{Emotes.SuccessEmote} Deleted RNG numbers");
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

    var user = (SocketGuildUser)cmd.User;
    await LogService.LogToFileAndConsole(
      $"User {user} is getting a fake rng number: {selectedNum}");

    await DatabaseService.NonQuery("DELETE FROM rngnums WHERE id = (SELECT id FROM rngnums LIMIT(1));");
  }

  private async Task PrintRealRng(long rngMin, long rngMax, SocketSlashCommand cmd)
  {
    if (rngMin > rngMax)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Minimum number cannot be bigger than maximum");
      return;
    }
    else if (rngMin == rngMax)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} The two numbers cannot be the same");
      return;
    }

    long rngNum = Random.Shared.NextInt64(rngMin, rngMax);
    await cmd.RespondAsync(rngNum.ToString());
  }
}
