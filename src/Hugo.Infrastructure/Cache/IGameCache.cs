using Hugo.Core.Models;

namespace Hugo.Infrastructure.Cache;

public interface IGameCache
{
    Task<Game?> GetGameAsync(string roomId);
    Task SetGameAsync(string roomId, Game game);
    Task RemoveGameAsync(string roomId);
    Task<bool> GameExistsAsync(string roomId);
    Task<List<string>> GetActiveGamesAsync();
} 