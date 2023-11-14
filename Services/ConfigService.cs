namespace Moe.Services;

public static class ConfigService
{
  public static CLIOptions Options { get; private set; } = default!;
  public static Environment Environment { get; private set; } = default!;

  public static async Task Init(string[] args)
  {
    Options = CLIOptions.Parse(args);
    Environment = await Moe.Environment.Load();

    var environment = Options.IsDevelopment ? "development" : "production";
    await LogService.LogToFileAndConsole($"Running in {environment} environment");
  }
}
