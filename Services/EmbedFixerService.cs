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

  private readonly Dictionary<string, string> linkRegexes;
  private readonly SettingsService settingsService;
  private Dictionary<ulong, List<EmbedFixerPattern>> patternsCache = new();

  public EmbedFixerService(SettingsService settingsService)
  {
    linkRegexes = new Dictionary<string, string>
    {
      { @"https?://(?:clips\.twitch\.tv|(?:www\.)?twitch\.tv/\w+\/clip)\/([A-Za-z0-9-_]+)", @"https://clips.fxtwitch.tv/$1"},
      { @"https?://(?:[\w-]+?\.)?reddit\.com", @"https://rxddit.com" },
      { @"https?://(?:www\.)?threads\.net", @"https://fixthreads.net" },
      { @"https?://(?:www\.)?(twitter|x)\.com", @"https://fxtwitter.com" },
      { @"https?://(?:www\.)?instagram.com", @"https://ddinstagram.com" },
      { @"https?://(?:to)?github\.com/([A-Za-z0-9-]+/[A-Za-z0-9._-]+)/(?:issues|pull)/([0-9]+)([^\s]*)?", @"[$1#$2$3]($&)" },
      { @"https?://(?:www\.)?tiktok.com", @"https://vxtiktok.com" },
    };

    InitializeDatabase().Wait();
    this.settingsService = settingsService;
  }

  public bool IsAuthorized(SocketGuildUser user, out string? error)
  {
    return settingsService.IsAuthorized(user, ModrankLevel.Administrator, out error);
  }

  public async Task<string> ReplaceLinks(ulong guildId, string input)
  {
    var patterns = await GetPatternsFromCache(guildId);
    foreach (var pattern in patterns)
    {
      Regex regex = new Regex(pattern.Pattern);
      input = regex.Replace(input, pattern.Replacement);
    }

    return input;
  }

  private async Task<List<EmbedFixerPattern>> GetPatterns(ulong guildId)
  {
    var sql = "SELECT pattern, replacement FROM embed_fixer WHERE guild_id = $0 AND pattern != $1";
    var result = await DatabaseService.Query<string, string>(sql, guildId, initializedMagic);
    return result.ConvertAll(x => new EmbedFixerPattern()
    {
      Pattern = x.Item1!,
      Replacement = x.Item2!
    });
  }

  private async Task<List<EmbedFixerPattern>> GetPatternsFromCache(ulong guildId)
  {
    if (patternsCache.TryGetValue(guildId, out var patterns))
    {
      return patterns;
    }

    var newPatterns = await GetPatterns(guildId);
    patternsCache.Add(guildId, newPatterns);
    return newPatterns;
  }

  private void InvalidatePatternsCache(ulong guildId)
  {
    patternsCache.Remove(guildId);
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

    foreach (var (pattern, replacement) in linkRegexes)
    {
      await DatabaseService.NonQuery(sql, guildId, pattern, replacement);
    }
  }
}
