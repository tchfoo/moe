using Discord;
using Discord.WebSocket;

var config = Config.Load();
var client = new DiscordSocketClient();

client.Log += async (msg) => Console.WriteLine(msg.ToString());

client.Ready += async () =>
{
  var guild = client.GetGuild(config.ServerID);
  var ping = new SlashCommandBuilder()
    .WithName("ping")
    .WithDescription("Test slash command");
  await guild.CreateApplicationCommandAsync(ping.Build());
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

await client.LoginAsync(TokenType.Bot, config.Token);
await client.StartAsync();

// Don't close the app
await Task.Delay(-1);
