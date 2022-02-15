using TNTBot.Commands;
using TNTBot.Services;
using Discord;

DiscordService.Init();

DiscordService.Discord.Ready += async () =>
{
  var muteService = new MuteService();
  var customCommandService = new CustomCommandService();
  var customRoleService = new CustomRoleService();
  var rngCommand = new RngCommand();
  var customCommandHandlerDM = new CustomCommandHandlerDM(customCommandService);
  var pinService = new PinService();

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
    new PinCommand(pinService),
  };

  var guild = DiscordService.Discord.GetGuild(ConfigService.Config.ServerID);

  if (args.Contains("--register-commands"))
  {
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

  var guildMessageCommand = new MessageCommandBuilder();
  guildMessageCommand.WithName("Pin to pin channel");
  await guild.CreateApplicationCommandAsync(guildMessageCommand.Build());

  DiscordService.Discord.MessageCommandExecuted += async (arg) =>
  {
    if (arg.CommandName == "Pin to pin channel")
    {
      await pinService.MessageCommandHandler(arg);
    }
  };
};

await DiscordService.Start();
