using Discord;
using TNTBot.Commands;
using TNTBot.Services;

SlashCommandBase[] commands = default!;

DiscordService.Discord.Log += (msg) =>
{
  Console.WriteLine(msg.ToString());
  return Task.CompletedTask;
};

DiscordService.Discord.Ready += async () =>
{
  var muteService = new MuteService();
  commands = new SlashCommandBase[]
  {
    new PingCommand(),
    new RngCommand(),
    new MuteCommand(muteService),
    new UnmuteCommand(muteService)
  };

  foreach (var command in commands)
  {
    await command.Register();
  }
};

DiscordService.Discord.SlashCommandExecuted += async (cmd) =>
{
  foreach (var command in commands!)
  {
    if (command.Name == cmd.CommandName)
    {
      await command.Handle(cmd);
      break;
    }
  }
};

DiscordService.Discord.MessageReceived += async (msg) =>
{
  if (msg.Channel.GetChannelType() == ChannelType.DM)
  {
    if (ConfigService.Config.Owners.Contains(msg.Author.Id) || msg.Author.Id == ConfigService.Config.Yaha)
    {
      if (msg.Content.StartsWith("!setrng"))
      {
        try
        {
          string tempRngMsg = msg.Content.Replace("!setrng", "").Trim();
          string[] rngMsg = tempRngMsg.Split(' ');

          List<string> rngMsgSelect = rngMsg
            .Select(x => int.Parse(x))
            .Select(x => $"({x})")
            .ToList();
          string rngMsgJoin = string.Join(',', rngMsgSelect);

          var addRngSql = $"INSERT INTO rngnums (num) VALUES {rngMsgJoin};";
          await DatabaseService.NonQuery(addRngSql);

          await msg.Channel.SendMessageAsync(rngMsgJoin);
        }
        catch (FormatException)
        {
          await msg.Channel.SendMessageAsync("Nem egész számokat adtál meg! Helyes szintaktika: `!setrng 10 20 30`");
        }
      }

      if (msg.Content.StartsWith("!clearrng"))
      {
        await DatabaseService.NonQuery("DELETE FROM rngnums;");

        await msg.Channel.SendMessageAsync("Rng számok sikeresen törölve.");
      }
    }
  }
};

await DiscordService.Discord.LoginAsync(TokenType.Bot, ConfigService.Token.Value);
await DiscordService.Discord.StartAsync();

// Don't close the app
await Task.Delay(-1);
