using CommandLine;

namespace Moe;

public class CLIOptions
{
  [Option('r', "register-commands", HelpText = "Register slash and context commands for the Discord bot.")]
  public bool RegisterCommands { get; set; } = false;

  [Option('d', "development", HelpText =
      $"Run the bot in development mode. Uses {Environment.DevelopmentEnvFile} file for configuration.\n" +
      "Command registration applies to specified guild when used with --register-commands.")]
  public bool IsDevelopment { get; set; } = false;

  [Option('p', "production", HelpText =
      $"Run the bot in production mode. Uses {Environment.ProductionEnvFile} file for configuration.\n" +
      "Command registration applies globally when used with --register-commands.\n" +
      "This option will be used when no --development or --production was specfied.")]
  public bool IsProduction { get; set; } = false;

  public static CLIOptions Parse(string[] args)
  {
    var parsed = Parser.Default.ParseArguments<CLIOptions>(args);
    var options = parsed.Value;
    if (options is null)
    {
      var errors = parsed.Errors
        .Select(e => e.Tag)
        .Except(new[] { ErrorType.HelpRequestedError, ErrorType.VersionRequestedError });
      System.Environment.Exit(errors.Any() ? 1 : 0);
    }

    if (options.IsDevelopment && options.IsProduction)
    {
      Console.WriteLine("Can't use both development and production mode at the same time.");
      System.Environment.Exit(1);
    }

    if (!options.IsDevelopment && !options.IsProduction)
    {
      options.IsProduction = true;
    }

    return options;
  }
}
