using System.Collections.Concurrent;
using Hugo.Core.Models;

namespace Hugo.Core.Services;

public class GameService : IGameService
{
    private readonly ConcurrentDictionary<string, Game> _games = new();

    public Game? GetGame(string gameId)
    {
        _games.TryGetValue(gameId, out var game);
        return game;
    }

    public Game CreateGame(string gameId, List<Player> players)
    {
        var game = new Game(gameId, players);
        _games.TryAdd(gameId, game);
        return game;
    }

    public void StartGame(string gameId)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            game.StartGame();
        }
        else
        {
            throw new InvalidOperationException("Oyun bulunamadı");
        }
    }

    public void AddPlayer(string gameId, Player player)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            var validPlayers = game.Players.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
            if (validPlayers.Count >= 4)
            {
                throw new InvalidOperationException("Oyun dolu");
            }

            // Aynı pozisyonda başka bir oyuncu var mı kontrol et
            if (validPlayers.Any(p => p.Position == player.Position))
            {
                throw new InvalidOperationException($"Pozisyon {player.Position} zaten kullanımda");
            }

            game.Players.Add(player);
        }
        else
        {
            throw new InvalidOperationException("Oyun bulunamadı");
        }
    }

    public void RemovePlayer(string gameId, string playerId)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
            {
                game.Players.Remove(player);
            }
        }
    }

    public void DrawStone(string gameId, string playerId)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            if (game.CurrentPlayerId != playerId)
            {
                throw new InvalidOperationException("Sıra sizde değil");
            }

            var stone = game.DrawStone();
            var player = game.Players.First(p => p.Id == playerId);
            player.AddStone(stone);
        }
        else
        {
            throw new InvalidOperationException("Oyun bulunamadı");
        }
    }

    public void ThrowStone(string gameId, string playerId, Stone stone)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            if (game.CurrentPlayerId != playerId)
            {
                throw new InvalidOperationException("Sıra sizde değil");
            }

            var player = game.Players.First(p => p.Id == playerId);
            if (player.RemoveStone(stone))
            {
                player.LastThrownStone = stone;
                game.NextTurn();
            }
        }
        else
        {
            throw new InvalidOperationException("Oyun bulunamadı");
        }
    }

    public void OpenPer(string gameId, string playerId, List<Stone> stones)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            var player = game.Players.First(p => p.Id == playerId);
            var per = new Per(stones, playerId);

            if (per.CalculateValue() < 51)
            {
                throw new InvalidOperationException("Per değeri en az 51 olmalıdır");
            }

            foreach (var stone in stones)
            {
                player.RemoveStone(stone);
            }

            game.OpenedPers.Add(per);
            player.HasOpenedHand = true;
        }
        else
        {
            throw new InvalidOperationException("Oyun bulunamadı");
        }
    }

    public void AddStoneToPer(string gameId, string playerId, Stone stone, int perId)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            var per = game.OpenedPers.FirstOrDefault(p => p.Id == perId);
            if (per == null)
            {
                throw new InvalidOperationException("Per bulunamadı");
            }

            if (!per.CanAddStone(stone))
            {
                throw new InvalidOperationException("Bu taş bu pere eklenemez");
            }

            var player = game.Players.First(p => p.Id == playerId);
            if (player.RemoveStone(stone))
            {
                per.AddStone(stone);
            }
        }
        else
        {
            throw new InvalidOperationException("Oyun bulunamadı");
        }
    }
} 