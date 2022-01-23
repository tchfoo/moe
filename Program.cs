using Discord;
using Discord.WebSocket;
using Microsoft.Data.Sqlite;

var connection = new SqliteConnection("Data Source=storage.db");
connection.Open();

var command = connection.CreateCommand();
command.CommandText =
@"
    CREATE TABLE IF NOT EXISTS rngnums(id INTEGER PRIMARY KEY AUTOINCREMENT, num INTEGER)
";

command.ExecuteNonQuery();
// using (var reader = command.ExecuteReader())
// {
//     while (reader.Read())
//     {
//         var name = reader.GetString(0);

//         Console.WriteLine($"Hello, {name}!");
//     }
// }

var config = Config.Load();
var token = Token.Load();
var client = new DiscordSocketClient();

client.Log += async (msg) => Console.WriteLine(msg.ToString());

client.Ready += async () =>
{
  var guild = client.GetGuild(config.ServerID);
  var ping = new SlashCommandBuilder()
    .WithName("ping")
    .WithDescription("Test slash command");
  await guild.CreateApplicationCommandAsync(ping.Build());

  var rng = new SlashCommandBuilder()
    .WithName("rng")
    .WithDescription("Generates a random number.")
    .AddOption("max", ApplicationCommandOptionType.Integer, "The maximum number the bot generates.", isRequired: true)
    .AddOption("min", ApplicationCommandOptionType.Integer, "The minimum number the bot generates. Defaults to 1.", isRequired: false);
  await guild.CreateApplicationCommandAsync(rng.Build());
};

client.SlashCommandExecuted += async (cmd) =>
{
  switch (cmd.CommandName)
  {
    case "ping":
      await cmd.RespondAsync("Pong!");
      break;
  }
};

client.MessageReceived += async (msg) =>
{
  if (msg.Channel.GetChannelType() == ChannelType.DM)
  {
    if (config.Owners.Contains(msg.Author.Id) || msg.Author.Id == config.Yaha)
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


          var command = connection.CreateCommand();
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
        var command = connection.CreateCommand();
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

await client.LoginAsync(TokenType.Bot, token.Value);
await client.StartAsync();

// Don't close the app
await Task.Delay(-1);
