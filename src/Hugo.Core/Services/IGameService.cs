using Hugo.Core.Models;

namespace Hugo.Core.Services;

public interface IGameService
{
    Game? GetGame(string gameId);
    Game CreateGame(string gameId, List<Player> players);
    void StartGame(string gameId);
    void AddPlayer(string gameId, Player player);
    void RemovePlayer(string gameId, string playerId);
    void DrawStone(string gameId, string playerId);
    void ThrowStone(string gameId, string playerId, Stone stone);
    void OpenPer(string gameId, string playerId, List<Stone> stones);
    void AddStoneToPer(string gameId, string playerId, Stone stone, int perId);
} 