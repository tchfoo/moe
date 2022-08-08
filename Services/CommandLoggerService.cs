using Discord.WebSocket;

namespace MoeBot.Services;

public class CommandLoggerService
{
  public async Task LogSlashCommand(SocketSlashCommand cmd)
  {
    var guild = ((SocketGuildChannel)cmd.Channel).Guild;
    var options = GetOptionsRecursively(cmd.Data.Options);
    var optionsString = options.Select(x => string.IsNullOrEmpty(x.Value?.ToString()) ? x.Name : $"{x.Name}:{x.Value}");
    var commandString = $"/{cmd.CommandName} {string.Join(" ", optionsString)}";
    await LogService.LogToFileAndConsole(
      $"{cmd.User} executed slash command {commandString}", guild);
  }

  private List<SocketSlashCommandDataOption> GetOptionsRecursively(IEnumerable<SocketSlashCommandDataOption> options)
  {
    var result = new List<SocketSlashCommandDataOption>();
    result.AddRange(options);
    foreach (var subOption in options)
    {
      result.AddRange(GetOptionsRecursively(subOption.Options));
    }
    return result;
  }
}
