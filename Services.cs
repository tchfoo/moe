
using Discord.WebSocket;
using Microsoft.Data.Sqlite;

namespace TNTBot
{
  public static class Services
  {
    public static SqliteConnection Connection { get; private set; }
    public static DiscordSocketClient Client { get; private set; }
    public static Config Config { get; private set; }

    public static void Init()
    {
      Connection = new SqliteConnection("Data Source=storage.db");
      Connection.Open();
      Client = new DiscordSocketClient();
      Config = Config.Load();
    }

    public static async Task ExecuteSqlNonQuery(string sql)
    {
      var command = Connection.CreateCommand();
      command.CommandText = sql;
      await command.ExecuteNonQueryAsync();
    }

    public static async Task<List<List<string>>> ExecuteSqlQuery(string sql)
    {
      var command = Connection.CreateCommand();
      command.CommandText = sql;
      using var reader = await command.ExecuteReaderAsync();
      var rows = new List<List<string>>();
      while (reader.Read())
      {
        var row = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
          row.Add(reader.GetString(i));
        }
        rows.Add(row);
      }
      return rows;
    }
  }
}
