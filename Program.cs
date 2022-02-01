using TNTBot.Commands;
using TNTBot.Services;

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
};

await DiscordService.Start();
