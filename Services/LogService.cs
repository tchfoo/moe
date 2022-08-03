using Discord;
using Discord.WebSocket;

namespace TNTBot.Services;

public class LogService
{
  public static LogService Instance { get; set; } = default!;

  private readonly SettingsService settingsService;

  public LogService(SettingsService settingsService)
  {
    this.settingsService = settingsService;
  }

  public async Task LogToDiscord(SocketGuild guild, string? text = null, Embed? embed = null)
  {
    var logChannel = await settingsService.GetLogChannel(guild);
    if (logChannel is null)
    {
      await LogToFileAndConsole("No log channel was set", guild, LogSeverity.Warning);
      return;
    }

    await logChannel.SendMessageAsync(text: text, embed: embed);
  }

  public static async Task LogToFileAndConsole(string message, SocketGuild? guild = null, LogSeverity severity = LogSeverity.Info)
  {
    var formattedMessage = FormatMessage(message, guild, severity);
    Console.WriteLine(formattedMessage);
    await WriteToLogFile(formattedMessage);
  }

  private static string FormatMessage(string message, SocketGuild? guild, LogSeverity severity)
  {
    var result = string.Empty;
    result += $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";
    result += $"[{severity}] ";
    if (guild is not null)
    {
      var guildName = guild.Name[0..Math.Min(5, guild.Name.Length)];
      var guildId = guild.Id.ToString()[0..5];
      result += $"[{guildName}#{guildId}] ";
    }
    return result + message;
  }

  private static async Task WriteToLogFile(string message)
  {
    var logsDirPath = "logs";
    Directory.CreateDirectory(logsDirPath);

    var currentLogPath = Path.Combine(logsDirPath, $"{DateTime.Now:yyyy-MM-dd}.log");
    await File.AppendAllTextAsync(currentLogPath, message + Environment.NewLine);
  }
}
