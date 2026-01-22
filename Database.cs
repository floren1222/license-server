using Microsoft.Data.SQLite;

public static class Database
{
    private const string Connection = "Data Source=licenses.db";

    public static void Init()
    {
        using var con = new SQLiteConnection(Connection);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS licenses (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            license_key TEXT UNIQUE,
            is_used INTEGER DEFAULT 0,
            hwid TEXT
        );
        ";
        cmd.ExecuteNonQuery();
    }
}
