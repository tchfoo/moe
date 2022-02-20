using TNTBot.Commands;
using TNTBot.Services;

DiscordService.Init();

DiscordService.Discord.Ready += async () =>
{
  var settingsService = new SettingsService();
  var muteService = new MuteService(settingsService);
  var customCommandService = new CustomCommandService();
  var customRoleService = new CustomRoleService();
  var levelService = new LevelService();
  var rngCommand = new RngCommand();
  var customCommandHandlerDM = new CustomCommandHandlerDM(customCommandService);
  var pinService = new PinService(settingsService);
  LogService.Instance = new LogService(settingsService);
  var discordLogService = new DiscordLogService();
  discordLogService.Register();

  var slashCommands = new List<SlashCommandBase>
  {
    new PingCommand(),
    rngCommand,
    new MuteCommand(muteService),
    new UnmuteCommand(muteService),
    new CustomCommandCommand(customCommandService),
    new SetCustomRoleCommand(customRoleService),
    new CustomRoleCommand(customRoleService),
    new ListCustomRolesCommand(customRoleService),
    new SettingsCommand(settingsService),
    new RankCommand(levelService),
    new LevelsCommand(levelService),
    new PinSlashCommand(pinService),
  };
  var messageCommands = new List<MessageCommandBase>
  {
    new PinMessageCommand(pinService),
  };

  var guild = DiscordService.Discord.GetGuild(ConfigService.Config.ServerID);

  if (args.Contains("--register-commands"))
  {
    await guild.DeleteApplicationCommandsAsync();
    foreach (var command in slashCommands)
    {
      await command.Register();
    }
    foreach (var command in messageCommands)
    {
      await command.Register();
    }
  }

  DiscordService.Discord.SlashCommandExecuted += async (cmd) =>
  {
    var command = slashCommands.First(x => cmd.CommandName == x.Name);
    await command.Handle(cmd);
  };

  DiscordService.Discord.MessageCommandExecuted += async (cmd) =>
  {
    var command = messageCommands.First(x => cmd.CommandName == x.Name);
    await command.Handle(cmd);
  };

  DiscordService.Discord.MessageReceived += async (msg) =>
  {
    if (msg.Author.IsBot)
    {
      return;
    }

    await levelService.HandleMessage(msg);

    var prefix = ConfigService.Config.CommandPrefix;
    if (!msg.Content.StartsWith(prefix))
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
