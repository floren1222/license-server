using Microsoft.Data.SQLite;

var builder = WebApplication.CreateBuilder(args);

// Добавляем OpenAPI (Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger для тестирования
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Инициализация базы данных
Database.Init();

// Endpoint для активации ключа
app.MapPost("/activate", (ActivationRequest req) =>
{
    using var con = new SQLiteConnection("Data Source=licenses.db");
    con.Open();

    // Проверяем, есть ли ключ
    var cmd = con.CreateCommand();
    cmd.CommandText = @"SELECT is_used, hwid FROM licenses WHERE license_key = $key";
    cmd.Parameters.AddWithValue("$key", req.Key);

    using var reader = cmd.ExecuteReader();
    if (!reader.Read())
        return Results.BadRequest(new { success = false, message = "INVALID_KEY" });

    bool used = reader.GetInt32(0) == 1;
    string savedHwid = reader.IsDBNull(1) ? "" : reader.GetString(1);

    // Если ключ уже активирован на другом ПК
    if (used && savedHwid != req.Hwid)
        return Results.BadRequest(new { success = false, message = "KEY_ALREADY_USED" });

    reader.Close();

    // Активируем ключ и привязываем к HWID
    var update = con.CreateCommand();
    update.CommandText = @"UPDATE licenses SET is_used = 1, hwid = $hwid WHERE license_key = $key";
    update.Parameters.AddWithValue("$key", req.Key);
    update.Parameters.AddWithValue("$hwid", req.Hwid);
    update.ExecuteNonQuery();

    return Results.Ok(new { success = true, message = "ACTIVATED" });
});

app.Run();

// --------------------------
// Класс для работы с БД
// --------------------------
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
        );";
        cmd.ExecuteNonQuery();
    }

    // Добавление ключа вручную (для админа)
    public static void AddKey(string key)
    {
        using var con = new SQLiteConnection(Connection);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT OR IGNORE INTO licenses (license_key) VALUES ($key)";
        cmd.Parameters.AddWithValue("$key", key);
        cmd.ExecuteNonQuery();
    }
}

// --------------------------
// Модель запроса
// --------------------------
public record ActivationRequest(string Key, string Hwid);
