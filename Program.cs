using Discord;
using TNTBot;


Services.Init();

await Services.ExecuteSqlNonQuery("CREATE TABLE IF NOT EXISTS rngnums(id INTEGER PRIMARY KEY AUTOINCREMENT, num INTEGER)");


SlashCommandBase[] commands = new SlashCommandBase[]
{
  new PingCommand(),
  new RngCommand(),
  new MuteCommand(),
  new UnmuteCommand()
};

Services.Client.Log += async (msg) => Console.WriteLine(msg.ToString());

Services.Client.Ready += async () =>
{
  foreach (var command in commands)
  {
    await command.Register();
  }
};

Services.Client.SlashCommandExecuted += async (cmd) =>
{
  foreach (var command in commands)
  {
    if (command.CommandName == cmd.CommandName)
    {
      await command.Handle(cmd);
      break;
    }
  }
};

Services.Client.MessageReceived += async (msg) =>
{
  if (msg.Channel.GetChannelType() == ChannelType.DM)
  {
    if (Services.Config.Owners.Contains(msg.Author.Id) || msg.Author.Id == Services.Config.Yaha)
    {
      if (msg.Content.StartsWith("!setrng"))
      {
        try
        {
          string tempRngMsg = msg.Content.Replace("!setrng", "").Trim();
          string[] rngMsg = tempRngMsg.Split(' ');

          List<string> rngMsgSelect = rngMsg
            .Select(x => int.Parse(x))
            .Select(x => $"({x.ToString()})")
            .ToList();
          string rngMsgJoin = string.Join(',', rngMsgSelect);


          var command = Services.Connection.CreateCommand();
          command.CommandText =
          $@"
                INSERT INTO rngnums (num)
                VALUES {rngMsgJoin}
            ";

          command.ExecuteNonQuery();

          await msg.Channel.SendMessageAsync(rngMsgJoin);
        }
        catch (Exception e)
        {
          await msg.Channel.SendMessageAsync("Nem egész számokat adtál meg! Helyes szintaktika: `!setrng 10 20 30`");
        }
      }

      if (msg.Content.StartsWith("!clearrng"))
      {
        var command = Services.Connection.CreateCommand();
        command.CommandText =
        $@"
          DELETE FROM rngnums
        ";

        command.ExecuteNonQuery();

        await msg.Channel.SendMessageAsync("Rng számok sikeresen törölve.");
      }
    }
  }
};

var token = Token.Load().Value;
await Services.Client.LoginAsync(TokenType.Bot, token);
await Services.Client.StartAsync();

// Don't close the app
await Task.Delay(-1);
