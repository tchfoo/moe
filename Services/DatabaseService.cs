using Microsoft.Data.Sqlite;

namespace TNTBot.Services
{
  public static class DatabaseService
  {
    private const string ConnectionString = "Data Source=storage.db";

    private static async Task<SqliteConnection> GetConnection()
    {
      var connection = new SqliteConnection(ConnectionString);
      await connection.OpenAsync();
      return connection;
    }

    private static SqliteCommand GetCommand(SqliteConnection connection, string sql, params object[] @params)
    {
      var command = connection.CreateCommand();
      command.CommandText = sql;
      for (int i = 0; i < @params.Length; i++)
      {
        command.Parameters.AddWithValue($"${i}", @params[i]);
      }
      return command;
    }

    private static T ConvertTo<T>(object value)
    {
      return (T)Convert.ChangeType(value, typeof(T));
    }

    public static async Task NonQuery(string sql, params object[] @params)
    {
      using var connection = await GetConnection();
      using var command = GetCommand(connection, sql, @params);
      await command.ExecuteNonQueryAsync();
    }

    public static async Task<List<List<string>>> Query(string sql, params object[] @params)
    {
      using var connection = await GetConnection();
      using var command = GetCommand(connection, sql, @params);
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

    public static async Task<string> QueryFirst(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query[0][0];
    }

    public static async Task<T> QueryFirst<T>(string sql, params object[] @params)
    {
      var query = await QueryFirst(sql, @params);
      return ConvertTo<T>(query);
    }

    public static async Task<List<T>> Query<T>(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query.ConvertAll(x => ConvertTo<T>(x[0]));
    }

    public static async Task<List<(T1, T2)>> Query<T1, T2>(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query.ConvertAll(x => (ConvertTo<T1>(x[0]), ConvertTo<T2>(x[1])));
    }

    public static async Task<List<(T1, T2, T3)>> Query<T1, T2, T3>(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query.ConvertAll(x => (ConvertTo<T1>(x[0]), ConvertTo<T2>(x[1]), ConvertTo<T3>(x[2])));
    }

    public static async Task<List<(T1, T2, T3, T4)>> Query<T1, T2, T3, T4>(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query.ConvertAll(x => (ConvertTo<T1>(x[0]), ConvertTo<T2>(x[1]), ConvertTo<T3>(x[2]), ConvertTo<T4>(x[3])));
    }

    public static async Task<List<(T1, T2, T3, T4, T5)>> Query<T1, T2, T3, T4, T5>(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query.ConvertAll(x => (ConvertTo<T1>(x[0]), ConvertTo<T2>(x[1]), ConvertTo<T3>(x[2]), ConvertTo<T4>(x[3]), ConvertTo<T5>(x[4])));
    }

    public static async Task<List<(T1, T2, T3, T4, T5, T6)>> Query<T1, T2, T3, T4, T5, T6>(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query.ConvertAll(x => (ConvertTo<T1>(x[0]), ConvertTo<T2>(x[1]), ConvertTo<T3>(x[2]), ConvertTo<T4>(x[3]), ConvertTo<T5>(x[4]), ConvertTo<T6>(x[5])));
    }

    public static async Task<List<(T1, T2, T3, T4, T5, T6, T7)>> Query<T1, T2, T3, T4, T5, T6, T7>(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query.ConvertAll(x => (ConvertTo<T1>(x[0]), ConvertTo<T2>(x[1]), ConvertTo<T3>(x[2]), ConvertTo<T4>(x[3]), ConvertTo<T5>(x[4]), ConvertTo<T6>(x[5]), ConvertTo<T7>(x[6])));
    }

    public static async Task<List<(T1, T2, T3, T4, T5, T6, T7, T8)>> Query<T1, T2, T3, T4, T5, T6, T7, T8>(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query.ConvertAll(x => (ConvertTo<T1>(x[0]), ConvertTo<T2>(x[1]), ConvertTo<T3>(x[2]), ConvertTo<T4>(x[3]), ConvertTo<T5>(x[4]), ConvertTo<T6>(x[5]), ConvertTo<T7>(x[6]), ConvertTo<T8>(x[7])));
    }

    public static async Task<List<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>> Query<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string sql, params object[] @params)
    {
      var query = await Query(sql, @params);
      return query.ConvertAll(x => (ConvertTo<T1>(x[0]), ConvertTo<T2>(x[1]), ConvertTo<T3>(x[2]), ConvertTo<T4>(x[3]), ConvertTo<T5>(x[4]), ConvertTo<T6>(x[5]), ConvertTo<T7>(x[6]), ConvertTo<T8>(x[7]), ConvertTo<T9>(x[8])));
    }
  }
}
