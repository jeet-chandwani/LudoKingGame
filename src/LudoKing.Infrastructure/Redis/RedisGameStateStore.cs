using System.Text.Json;
using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Infrastructure;
using StackExchange.Redis;

namespace LudoKing.Infrastructure.Redis;

public sealed class RedisGameStateStore : IGameStateStore
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(4);
    private readonly IDatabase _db;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public RedisGameStateStore(IConnectionMultiplexer redis) =>
        _db = redis.GetDatabase();

    public async Task<GameStateDto?> GetAsync(string gameId)
    {
        RedisValue value = await _db.StringGetAsync(gameId);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<GameStateDto>(value.ToString(), JsonOpts);
    }

    public Task SaveAsync(string gameId, GameStateDto state, TimeSpan? expiry = null)
    {
        string json = JsonSerializer.Serialize(state, JsonOpts);
        return _db.StringSetAsync(gameId, json, expiry ?? DefaultExpiry);
    }

    public Task DeleteAsync(string gameId) =>
        _db.KeyDeleteAsync(gameId);

    public async Task<bool> ExistsAsync(string gameId) =>
        await _db.KeyExistsAsync(gameId);
}
