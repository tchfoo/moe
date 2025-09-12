using System.Text.RegularExpressions;
using Discord.WebSocket;
using Moe.Models;

namespace Moe.Services;

public class EmbedFixerService
{
  // This ensures the user can delete all the regexes without being regenerated
  // When populating the embed_fixer table for a guild, insert this magic
  // Only populate when this magic is not set
  // Should be hidden from the user
  private static readonly string initializedMagic = "_initialized-634761";

  private readonly List<EmbedFixerPattern> initialPatterns;
  private readonly SettingsService settingsService;
  private Dictionary<ulong, List<EmbedFixerPattern>> patternsCache = new();

  public EmbedFixerService(SettingsService settingsService)
  {
    initialPatterns = new List<EmbedFixerPattern>
    {
      new(@"https?://(?:clips\.twitch\.tv|(?:www\.)?twitch\.tv/\w+\/clip)\/([A-Za-z0-9-_]+)", @"https://clips.fxtwitch.tv/$1"),
      new(@"https?://(?:[\w-]+?\.)?reddit\.com", @"https://rxddit.com"),
      new(@"https?://(?:www\.)?threads\.net", @"https://fixthreads.net"),
      new(@"https?://(?:www\.)?(twitter|x)\.com", @"https://fxtwitter.com"),
      new(@"https?://(?:www\.)?instagram.com", @"https://ddinstagram.com"),
      new(@"https?://(?:to)?github\.com/([A-Za-z0-9-]+/[A-Za-z0-9._-]+)/(?:issues|pull)/([0-9]+)([^\s]*)?", @"[$1#$2$3]($&)"),
      new(@"https?://([\w-]+\.)?tiktok.com", @"https://$1vxtiktok.com"),
    };

    InitializeDatabase().Wait();
    this.settingsService = settingsService;
  }

  public bool IsAuthorized(SocketGuildUser user, ModrankLevel requiredLevel, out string? error)
  {
    return settingsService.IsAuthorized(user, requiredLevel, out error);
  }

  public async Task<string> ReplaceLinks(SocketGuild guild, string input)
  {
    var patterns = await GetPatternsFromCache(guild);
    foreach (var pattern in patterns)
    {
      Regex regex = new Regex(pattern.Pattern);
      input = regex.Replace(input, pattern.Replacement);
    }

    return input;
  }

  private async Task<List<EmbedFixerPattern>> GetPatterns(SocketGuild guild)
  {
    var sql = "SELECT pattern, replacement FROM embed_fixer WHERE guild_id = $0 AND pattern != $1";
    var result = await DatabaseService.Query<string, string>(sql, guild.Id, initializedMagic);
    return result.ConvertAll(x => new EmbedFixerPattern(x.Item1!, x.Item2!));
  }

  public async Task<List<EmbedFixerPattern>> GetPatternsFromCache(SocketGuild guild)
  {
    if (patternsCache.TryGetValue(guild.Id, out var patterns))
    {
      return patterns;
    }

    var newPatterns = await GetPatterns(guild);
    patternsCache.Add(guild.Id, newPatterns);
    return newPatterns;
  }

  public async Task<bool> HasPatternInCache(SocketGuild guild, string pattern)
  {
    var patterns = await GetPatternsFromCache(guild);
    return patterns.Exists(x => x.Pattern == pattern);
  }

  public async Task AddPattern(SocketGuild guild, EmbedFixerPattern pattern)
  {
    InvalidatePatternsCache(guild);

    await LogService.LogToFileAndConsole($"Adding embed fixer pattern {pattern.Pattern} with replacement {pattern.Replacement}", guild);

    var sql = "INSERT INTO embed_fixer (guild_id, pattern, replacement) VALUES($0, $1, $2)";
    await DatabaseService.NonQuery(sql, guild.Id, pattern.Pattern, pattern.Replacement);
  }

  public async Task RemovePattern(SocketGuild guild, EmbedFixerPattern pattern)
  {
    InvalidatePatternsCache(guild);

    await LogService.LogToFileAndConsole($"Removing embed fixer pattern {pattern.Pattern} with replacement {pattern.Replacement}", guild);

    var sql = "DELETE FROM embed_fixer WHERE guild_id = $0 AND pattern = $1 AND replacement = $2";
    await DatabaseService.NonQuery(sql, guild.Id, pattern.Pattern, pattern.Replacement);
  }

  private void InvalidatePatternsCache(SocketGuild guild)
  {
    patternsCache.Remove(guild.Id);
  }

  private async Task InitializeDatabase()
  {
    await CreateEmbedFixerTable();

    var joinedGuilds = DiscordService.Discord.Guilds.Select(x => x.Id);
    var initializedGuilds = await GetInitializedGuilds();
    var uninitializedGuilds = joinedGuilds.Except(initializedGuilds);

    foreach (var guildId in uninitializedGuilds)
    {
      await PopulateEmbedFixerTable(guildId);
    }
  }

  private async Task<List<ulong>> GetInitializedGuilds()
  {
    var sql = "SELECT guild_id FROM embed_fixer WHERE pattern = $0";
    var result = await DatabaseService.Query<ulong>(sql, initializedMagic);
    if (result == null)
    {
      return new();
    }

    return result;
  }

  private async Task CreateEmbedFixerTable()
  {
    var sql = @"
      CREATE TABLE IF NOT EXISTS embed_fixer(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        guild_id INTEGER NOT NULL,
        pattern TEXT NOT NULL,
        replacement TEXT NOT NULL
      )";
    await DatabaseService.NonQuery(sql);
  }

  private async Task PopulateEmbedFixerTable(ulong guildId)
  {
    var sql = @"
      INSERT INTO embed_fixer (guild_id, pattern, replacement)
      VALUES($0, $1, $2)";

    await DatabaseService.NonQuery(sql, guildId, initializedMagic, string.Empty);

    foreach (var pattern in initialPatterns)
    {
      await DatabaseService.NonQuery(sql, guildId, pattern.Pattern, pattern.Replacement);
    }
  }
}
