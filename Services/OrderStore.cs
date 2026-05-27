using System.Collections.Concurrent;
using System.Text.Json;
using DogBed.Models;
using Npgsql;

namespace DogBed.Services;

public class OrderStore
{
    private readonly ConcurrentDictionary<string, FulfillmentOrder> _cache = new();
    private readonly string? _connStr;
    private readonly ILogger<OrderStore> _logger;

    public OrderStore(IConfiguration config, ILogger<OrderStore> logger)
    {
        _logger  = logger;
        _connStr = config["DATABASE_URL"];

        if (_connStr != null)
            InitDb().GetAwaiter().GetResult();
    }

    private async Task InitDb()
    {
        try
        {
            await using var conn = await OpenAsync();

            // Create table if it doesn't exist
            await using var create = conn.CreateCommand();
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS orders (
                    session_id TEXT PRIMARY KEY,
                    data       TEXT NOT NULL,
                    created_at TIMESTAMPTZ DEFAULT NOW()
                )
                """;
            await create.ExecuteNonQueryAsync();

            // Load all existing orders into memory cache
            await using var select = conn.CreateCommand();
            select.CommandText = "SELECT data FROM orders ORDER BY created_at DESC";
            await using var reader = await select.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                try
                {
                    var order = JsonSerializer.Deserialize<FulfillmentOrder>(reader.GetString(0));
                    if (order != null) _cache[order.StripeSessionId] = order;
                }
                catch { }
            }

            _logger.LogInformation("OrderStore: loaded {Count} orders from database", _cache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderStore: failed to connect to database — running in-memory only");
        }
    }

    public void Save(FulfillmentOrder order)
    {
        _cache[order.StripeSessionId] = order;
        _ = PersistAsync(order);
    }

    private async Task PersistAsync(FulfillmentOrder order)
    {
        if (_connStr == null) return;
        try
        {
            var json = JsonSerializer.Serialize(order);
            await using var conn = await OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO orders (session_id, data)
                VALUES (@sid, @data)
                ON CONFLICT (session_id) DO UPDATE SET data = @data
                """;
            cmd.Parameters.AddWithValue("sid",  order.StripeSessionId);
            cmd.Parameters.AddWithValue("data", json);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderStore: failed to persist order {Id}", order.Id);
        }
    }

    public FulfillmentOrder? GetBySessionId(string sessionId) =>
        _cache.GetValueOrDefault(sessionId);

    public IReadOnlyList<FulfillmentOrder> GetAll() =>
        _cache.Values.OrderByDescending(o => o.CreatedAt).ToList();

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(_connStr) { SslMode = SslMode.Require };
        var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();
        return conn;
    }
}
