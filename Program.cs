using Discord.WebSocket;
using TNTBot.Commands;
using TNTBot.Services;

DiscordService.Init();

DiscordService.Discord.Ready += async () =>
{
  var settingsService = new SettingsService();
  var customCommandService = new CustomCommandService(settingsService);
  var customRoleService = new CustomRoleService(settingsService);
  var levelService = new LevelService();
  var rngCommand = new RngCommand();
  var customCommandHandlerDM = new CustomCommandHandlerDM(customCommandService);
  var pinService = new PinService(settingsService);
  LogService.Instance = new LogService(settingsService);
  var discordLogService = new DiscordLogService();
  discordLogService.Register();
  var roleRememberService = new RoleRememberService();
  await roleRememberService.Register();
  var purgeService = new PurgeService(settingsService);
  var sayService = new SayService(settingsService);
  var userInfoService = new UserInfoService();
  var templateService = new TemplateService(settingsService);

  var slashCommands = new List<SlashCommandBase>
  {
    new PingCommand(),
    rngCommand,
    new CustomCommandCommand(customCommandService),
    new CustomRoleCommand(customRoleService),
    new SettingsCommand(settingsService),
    new RankCommand(levelService),
    new LevelsCommand(levelService),
    new PinSlashCommand(pinService),
    new LevelupMessage(levelService),
    new PurgeCommand(purgeService),
    new SayCommand(sayService),
    new UserinfoCommand(userInfoService),
    new ServerinfoCommand(settingsService),
    new TemplateCommand(templateService),
    new AnnounceCommand(templateService),
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
    var guild = ((SocketGuildChannel)cmd.Channel).Guild;
    var optionsString = cmd.Data.Options.Select(x => $"{x.Name}:{x.Value}");
    var commandString = $"/{cmd.CommandName} {string.Join(" ", optionsString)}";
    await LogService.LogToFileAndConsole(
      $"{cmd.User} executed slash command {commandString}", guild);

    var command = slashCommands.First(x => cmd.CommandName == x.Name);
    await command.Handle(cmd);
  };

  DiscordService.Discord.MessageCommandExecuted += async (cmd) =>
  {
    var guild = ((SocketGuildChannel)cmd.Channel).Guild;
    await LogService.LogToFileAndConsole(
      $"{cmd.User} executed message command {cmd.CommandName} on message {cmd.Data.Message}", guild);

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

    var guild = ((SocketGuildChannel)msg.Channel).Guild;
    var prefix = await settingsService.GetCommandPrefix(guild);
    if (!msg.Content.StartsWith(prefix))
    {
      return;
    }

    var tokens = msg.Content.Split(' ');
    var name = await customCommandService.CleanCommandName(guild, tokens[0]);
    var args = tokens.Skip(1).ToList();

    if (await rngCommand.HandleDM(msg, name, args))
    {
      return;
    }

    await customCommandHandlerDM.TryHandleCommand(msg, name, args);
  };
};

await DiscordService.Start();
