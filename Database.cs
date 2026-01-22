using Microsoft.Data.Sqlite;

public static class Database
{
    private const string Connection = "Data Source=licenses.db";

    public static void Init()
    {
        using var con = new SqliteConnection(Connection);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS licenses (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            license_key TEXT UNIQUE,
            is_used INTEGER DEFAULT 0,
            hwid TEXT
        );";
        cmd.ExecuteNonQuery();
    }

    public static void AddKey(string key)
    {
        using var con = new SqliteConnection(Connection);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT OR IGNORE INTO licenses (license_key) VALUES ($key)";
        cmd.Parameters.AddWithValue("$key", key);
        cmd.ExecuteNonQuery();
    }
}
