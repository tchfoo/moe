using System.Text.RegularExpressions;
using Discord.WebSocket;
using TNTBot.Models;

namespace TNTBot.Services
{
  public class CustomCommandService
  {
    private readonly SettingsService settingsService;

    public CustomCommandService(SettingsService settingsService)
    {
      CreateCustomCommandsTable().Wait();
      this.settingsService = settingsService;
    }

    public bool IsAuthorized(SocketGuildUser user, ModrankLevel requiredLevel, out string? error)
    {
      return settingsService.IsAuthorized(user, requiredLevel, out error);
    }

    public async Task<bool> HasCommands(SocketGuild guild)
    {
      var sql = "SELECT COUNT(*) FROM custom_commands WHERE guild_id = $0";
      var count = await DatabaseService.QueryFirst<int>(sql, guild.Id);
      return count > 0;
    }

    public async Task<bool> HasCommand(SocketGuild guild, string name)
    {
      name = await CleanCommandName(guild, name);
      var sql = "SELECT COUNT(*) FROM custom_commands WHERE guild_id = $0 AND name = $1";
      var count = await DatabaseService.QueryFirst<int>(sql, guild.Id, name);
      return count > 0;
    }

    public async Task<List<CustomCommand>> GetCommands(SocketGuild guild)
    {
      var sql = "SELECT name, response, description, delete_sender FROM custom_commands WHERE guild_id = $0 ORDER BY name";
      var commands = await DatabaseService.Query<string, string, string, int>(sql, guild.Id);
      return commands.ConvertAll(x => new CustomCommand(x.Item1!, x.Item2!, x.Item3, x.Item4 > 0));
    }

    public async Task<CustomCommand?> GetCommand(SocketGuild guild, string name)
    {
      name = await CleanCommandName(guild, name);
      var sql = "SELECT response, description, delete_sender FROM custom_commands WHERE guild_id = $0 AND name = $1";
      var command = await DatabaseService.Query<string, string, int>(sql, guild.Id, name);
      if (command.Count == 0)
      {
        return null;
      }

      return new CustomCommand(name, command[0].Item1!, command[0].Item2, command[0].Item3 > 0);
    }

    public async Task AddCommand(SocketGuild guild, string name, string response, string? description, bool delete)
    {
      name = await CleanCommandName(guild, name);
      await LogService.LogToFileAndConsole(
        $"Adding custom command {name} response: {response}, description: {description}, delete: {delete}", guild);

      var sql = "INSERT INTO custom_commands(guild_id, name, response, description, delete_sender) VALUES($0, $1, $2, $3, $4)";
      await DatabaseService.NonQuery(sql, guild.Id, name, response, description, delete);
    }

    public async Task RemoveCommand(SocketGuild guild, string name)
    {
      name = await CleanCommandName(guild, name);
      await LogService.LogToFileAndConsole(
        $"Removing custom command {name}", guild);

      var sql = "DELETE FROM custom_commands WHERE guild_id = $0 AND name = $1";
      await DatabaseService.NonQuery(sql, guild.Id, name);
    }

    public List<ParameterErrorInfo> GetParameterErrorInfos(string response)
    {
      return Regex.Matches(response, @"\$(\d+)")
        .Select(x => new ParameterErrorInfo()
        {
          DollarIndex = int.Parse(x.Groups[1].Value),
          ResponseStartIndex = x.Index,
          ResponseEndIndex = x.Index + x.Length
        })
        .DistinctBy(x => x.DollarIndex)
        .OrderBy(x => x.DollarIndex)
        .ToList();
    }

    public async Task<string> CleanCommandName(SocketGuild guild, string name)
    {
      var prefix = Regex.Escape(await settingsService.GetCommandPrefix(guild));
      return Regex.Replace(name, $"^({prefix})+", "");
    }

    public async Task<string> PrefixCommandName(SocketGuild guild, string name)
    {
      return await settingsService.GetCommandPrefix(guild) + await CleanCommandName(guild, name);
    }

    private async Task CreateCustomCommandsTable()
    {
      var sql = @"
        CREATE TABLE IF NOT EXISTS custom_commands(
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          guild_id INTEGER NOT NULL,
          name TEXT NOT NULL,
          response TEXT NOT NULL,
          description TEXT,
          delete_sender INTEGER NOT NULL DEFAULT 0
        )";
      await DatabaseService.NonQuery(sql);
    }
  }
}
