using Hugo.Core.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Hugo.Infrastructure.Cache;

public class RedisGameCache : IGameCache
{
    private readonly IDistributedCache _cache;
    private const string KeyPrefix = "game:";

    public RedisGameCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<Game?> GetGameAsync(string roomId)
    {
        var json = await _cache.GetStringAsync($"{KeyPrefix}{roomId}");
        return json == null ? null : JsonSerializer.Deserialize<Game>(json);
    }

    public async Task SetGameAsync(string roomId, Game game)
    {
        var json = JsonSerializer.Serialize(game);
        await _cache.SetStringAsync($"{KeyPrefix}{roomId}", json);
    }

    public async Task RemoveGameAsync(string roomId)
    {
        await _cache.RemoveAsync($"{KeyPrefix}{roomId}");
    }

    public async Task<bool> GameExistsAsync(string roomId)
    {
        var game = await GetGameAsync(roomId);
        return game != null;
    }

    public async Task<List<string>> GetActiveGamesAsync()
    {
        // TODO: Redis'te aktif oyunları listeleme mantığı eklenecek
        return new List<string>();
    }
} 