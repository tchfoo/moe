using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.ComponentModel.Design;
using TNTBot.Commands;
using TNTBot.Services;
using Discord;
using Discord.WebSocket;

DiscordService.Init();

DiscordService.Discord.Ready += async () =>
{
  var muteService = new MuteService();
  var customCommandService = new CustomCommandService();
  var customRoleService = new CustomRoleService();
  var rngCommand = new RngCommand();
  var customCommandHandlerDM = new CustomCommandHandlerDM(customCommandService);

  var commands = new List<SlashCommandBase>
  {
    new PingCommand(),
    rngCommand,
    new MuteCommand(muteService),
    new UnmuteCommand(muteService),
    new CustomCommandCommand(customCommandService),
    new SetCustomRoleCommand(customRoleService),
    new CustomRoleCommand(customRoleService),
    new ListCustomRolesCommand(customRoleService),
    new PinCommand(),
};

  if (args.Contains("--register-commands"))
  {
    var guild = DiscordService.Discord.GetGuild(ConfigService.Config.ServerID);
    await guild.DeleteApplicationCommandsAsync();
    foreach (var command in commands)
    {
      await command.Register();
    }
  }

  DiscordService.Discord.SlashCommandExecuted += async (cmd) =>
  {
    var command = commands.First(x => cmd.CommandName == x.Name);
    await command.Handle(cmd);
  };

  DiscordService.Discord.MessageReceived += async (msg) =>
  {
    var prefix = ConfigService.Config.CommandPrefix;
    if (!msg.Content.StartsWith(prefix) || msg.Author.IsBot)
    {
      return;
    }

    var tokens = msg.Content.Split(' ');
    var name = customCommandService.CleanCommandName(tokens[0]);
    var args = tokens.Skip(1).ToList();

    if (await rngCommand.HandleDM(msg, name, args))
    {
      return;
    }

    await customCommandHandlerDM.TryHandleCommand(msg, name, args);
  };

  var guild2 = DiscordService.Discord.GetGuild(ConfigService.Config.ServerID);
  var guildMessageCommand = new MessageCommandBuilder();
  guildMessageCommand.WithName("Pin to pin channel");
  await guild2.CreateApplicationCommandAsync(guildMessageCommand.Build());
};

DiscordService.Discord.MessageCommandExecuted += MessageCommandHandler;

static async Task MessageCommandHandler(SocketMessageCommand arg)
{
  if (arg.CommandName == "Pin to pin channel")
  {
    var pinChannel = (SocketTextChannel)DiscordService.Discord.GetChannel(938860776591089674);

    await arg.RespondAsync("Message successfully pinned.");
    var roles = ((SocketGuildUser)arg.Data.Message.Author).Roles
      .Where(x => x.Color != Color.Default)
      .OrderBy(x => x.Position);

    if (arg.Data.Message.Attachments.Any())
    {
      var embed = new EmbedBuilder()
      .WithAuthor(arg.Data.Message.Author)
      .WithImageUrl(arg.Data.Message.Attachments.First().Url)
      .WithDescription($"{arg.Data.Message}\n\n[Jump to message]({arg.Data.Message.GetJumpUrl()})")
      .WithFooter($"{arg.Data.Message.Timestamp.DateTime.ToString("yyyy-MM-dd • H:m")}")
      .WithColor(roles.Last().Color);

      await pinChannel.SendMessageAsync(embed: embed.Build());
    }
    else
    {
      var embed = new EmbedBuilder()
      .WithAuthor(arg.Data.Message.Author)
      .WithDescription($"{arg.Data.Message}\n\n[Jump to message]({arg.Data.Message.GetJumpUrl()})")
      .WithFooter($"{arg.Data.Message.Timestamp.DateTime.ToString("yyyy-MM-dd • H:m")}")
      .WithColor(roles.Last().Color);

      await pinChannel.SendMessageAsync(embed: embed.Build());
    }
  }
}

await DiscordService.Start();
