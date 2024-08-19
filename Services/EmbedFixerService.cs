using System.Text.RegularExpressions;

namespace Moe.Services;

public class EmbedFixerService
{
  // This ensures the user can delete all the regexes without being regenerated
  // When populating the embed_fixer table for a guild, insert this magic
  // Only populate when this magic is not set
  // Should be hidden from the user
  private static readonly string initializedMagic = "_initialized-634761";

  private readonly Dictionary<string, string> linkRegexes;

  public EmbedFixerService()
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
  }

  public string ReplaceLinks(string input)
  {
    foreach (var (key, value) in linkRegexes)
    {
      Regex regex = new Regex(key);
      input = regex.Replace(input, value);
    }

    return input;
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
