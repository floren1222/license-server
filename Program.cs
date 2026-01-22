using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LicenseServer API", Version = "v1" });
});

var app = builder.Build();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LicenseServer API v1");
    });
}

app.UseHttpsRedirection();

// Инициализация базы
Database.Init();

// Endpoint активации
app.MapPost("/activate", (ActivationRequest req) =>
{
    using var con = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=licenses.db");
    con.Open();

    var cmd = con.CreateCommand();
    cmd.CommandText = @"SELECT is_used, hwid FROM licenses WHERE license_key = $key";
    cmd.Parameters.AddWithValue("$key", req.Key);

    using var reader = cmd.ExecuteReader();
    if (!reader.Read())
        return Results.BadRequest(new { success = false, message = "INVALID_KEY" });

    bool used = reader.GetInt32(0) == 1;
    string savedHwid = reader.IsDBNull(1) ? "" : reader.GetString(1);

    if (used && savedHwid != req.Hwid)
        return Results.BadRequest(new { success = false, message = "KEY_ALREADY_USED" });

    reader.Close();

    var update = con.CreateCommand();
    update.CommandText = @"UPDATE licenses SET is_used = 1, hwid = $hwid WHERE license_key = $key";
    update.Parameters.AddWithValue("$key", req.Key);
    update.Parameters.AddWithValue("$hwid", req.Hwid);
    update.ExecuteNonQuery();

    return Results.Ok(new { success = true, message = "ACTIVATED" });
});

app.Run();

public record ActivationRequest(string Key, string Hwid);
