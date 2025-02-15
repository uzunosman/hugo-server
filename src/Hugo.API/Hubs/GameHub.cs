using Microsoft.AspNetCore.SignalR;
using Hugo.Core.Models;
using Hugo.Core.Services;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Hugo.API.Hubs;

public class GameHub : Hub
{
    private static readonly ConcurrentDictionary<string, Game> _games = new();
    private static readonly ConcurrentDictionary<string, string> _userGameMap = new();
    private readonly ILogger<GameHub> _logger;
    private readonly IGameService _gameService;

    public GameHub(ILogger<GameHub> logger, IGameService gameService)
    {
        _logger = logger;
        _gameService = gameService;
    }

    public async Task CreateGame(string playerName)
    {
        var gameId = Guid.NewGuid().ToString();
        var player = new Player(Context.ConnectionId, playerName) { Position = 0 };
        var game = _gameService.CreateGame(gameId, new List<Player> { player });
        
        _userGameMap.TryAdd(Context.ConnectionId, game.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);
        await Clients.Caller.SendAsync("GameCreated", game.Id);
        await NotifyGameState(game);
    }

    public async Task JoinGame(string gameId, string playerName)
    {
        try
        {
            _logger.LogInformation($"Oyuna katılma isteği - GameId: {gameId}, PlayerName: {playerName}");
            
            var game = _gameService.GetGame(gameId);
            if (game == null)
            {
                _logger.LogWarning($"Oyun bulunamadı - GameId: {gameId}");
                await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
                return;
            }

            _logger.LogInformation($"Mevcut oyuncu sayısı: {game.Players.Count}");
            var validPlayers = game.Players.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
            if (validPlayers.Count >= 4)
            {
                _logger.LogWarning($"Oyun dolu - GameId: {gameId}, Oyuncu sayısı: {validPlayers.Count}");
                await Clients.Caller.SendAsync("Error", "Oyun dolu.");
                return;
            }

            // Kullanılan pozisyonları kontrol et
            var usedPositions = validPlayers.Select(p => p.Position).ToList();
            _logger.LogInformation($"Kullanılan pozisyonlar: {string.Join(", ", usedPositions)}");

            // Boş pozisyonu bul
            var availablePositions = Enumerable.Range(0, 4).Except(usedPositions).ToList();
            if (!availablePositions.Any())
            {
                _logger.LogError($"Boş pozisyon bulunamadı - GameId: {gameId}");
                await Clients.Caller.SendAsync("Error", "Oyuna katılım sırasında bir hata oluştu.");
                return;
            }

            var position = availablePositions.First();
            _logger.LogInformation($"Yeni oyuncu pozisyonu: {position}");

            var player = new Player(Context.ConnectionId, playerName) { Position = position };
            _gameService.AddPlayer(gameId, player);
            _userGameMap.TryAdd(Context.ConnectionId, gameId);
            
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerJoined", new { player.Name, Position = position });
            
            // Güncel oyun durumunu al ve bildir
            game = _gameService.GetGame(gameId);
            await NotifyGameState(game);

            _logger.LogInformation($"Oyuncu başarıyla katıldı - GameId: {gameId}, PlayerName: {playerName}, Position: {position}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Oyuna katılma hatası - GameId: {gameId}, Error: {ex.Message}");
            await Clients.Caller.SendAsync("Error", "Oyuna katılım sırasında bir hata oluştu: " + ex.Message);
        }
    }

    public async Task DrawStone()
    {
        if (!_userGameMap.TryGetValue(Context.ConnectionId, out var gameId))
        {
            await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
            return;
        }

        try
        {
            _gameService.DrawStone(gameId, Context.ConnectionId);
            var game = _gameService.GetGame(gameId);
            var player = game.Players.First(p => p.Id == Context.ConnectionId);
            var stone = player.Stones.Last(); // Son çekilen taş

            await Clients.Caller.SendAsync("StoneDrawn", stone);
            await Clients.Group(gameId).SendAsync("PlayerDrewStone", player.Name);
            await NotifyGameState(game);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ThrowStone(Stone stone)
    {
        if (!_userGameMap.TryGetValue(Context.ConnectionId, out var gameId))
        {
            await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
            return;
        }

        try
        {
            _gameService.ThrowStone(gameId, Context.ConnectionId, stone);
            var game = _gameService.GetGame(gameId);
            var player = game.Players.First(p => p.Id == Context.ConnectionId);

            await Clients.Group(gameId).SendAsync("StoneThrown", stone, player.Name, player.Position);
            await NotifyGameState(game);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task OpenPer(List<Stone> stones)
    {
        if (!_userGameMap.TryGetValue(Context.ConnectionId, out var gameId))
        {
            await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
            return;
        }

        try
        {
            _gameService.OpenPer(gameId, Context.ConnectionId, stones);
            var game = _gameService.GetGame(gameId);
            var player = game.Players.First(p => p.Id == Context.ConnectionId);
            var per = game.OpenedPers.Last(); // Son açılan per

            await Clients.Group(gameId).SendAsync("PerOpened", per, player.Name);
            await NotifyGameState(game);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task AddStoneToPer(Stone stone, int perId)
    {
        if (!_userGameMap.TryGetValue(Context.ConnectionId, out var gameId))
        {
            await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
            return;
        }

        try
        {
            _gameService.AddStoneToPer(gameId, Context.ConnectionId, stone, perId);
            var game = _gameService.GetGame(gameId);
            var player = game.Players.First(p => p.Id == Context.ConnectionId);

            await Clients.Group(gameId).SendAsync("StoneAddedToPer", stone, perId, player.Name);
            await NotifyGameState(game);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task StartGame(string roomId)
    {
        _logger.LogInformation("Oyun başlatma isteği: RoomId={RoomId}", roomId);

        var game = _gameService.GetGame(roomId);
        if (game == null)
        {
            throw new HubException("Oyun bulunamadı.");
        }

        var validPlayers = game.Players.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
        _logger.LogInformation($"Geçerli oyuncu sayısı: {validPlayers.Count}");

        try
        {
            if (validPlayers.Count != 4)
            {
                _logger.LogError($"Yetersiz oyuncu sayısı: {validPlayers.Count}");
                throw new HubException("Oyun başlatmak için 4 oyuncu gerekli.");
            }

            // Taşları oluştur ve karıştır
            var stones = CreateAndShuffleStones();
            _logger.LogInformation($"Taşlar oluşturuldu: {stones.Count} adet");
            
            // Taşları dağıt (her oyuncuya 14, sağdaki oyuncuya 15)
            var playerStones = new Dictionary<string, List<Stone>>();
            var currentIndex = 0;
            
            for (var i = 0; i < validPlayers.Count; i++)
            {
                var stoneCount = i == 0 ? 15 : 14; // İlk oyuncuya 15, diğerlerine 14 taş
                var playerStoneList = stones.Skip(currentIndex).Take(stoneCount).ToList();
                playerStones[validPlayers[i].Id] = playerStoneList;
                _logger.LogInformation($"Oyuncu {i + 1} ({validPlayers[i].Name}): {stoneCount} taş dağıtıldı");
                currentIndex += stoneCount;
            }

            // Gösterge taşını belirle
            var indicatorStone = stones[currentIndex];
            var okeyStone = DetermineOkeyStone(indicatorStone);
            _logger.LogInformation($"Gösterge taşı: {indicatorStone}, Okey taşı: {okeyStone}");
            
            // Kalan taşları desteye koy
            var remainingStones = stones.Skip(currentIndex + 1).ToList();
            _logger.LogInformation($"Kalan taş sayısı: {remainingStones.Count}");

            game.Start(playerStones, indicatorStone, okeyStone, remainingStones);
            _logger.LogInformation("Oyun başlatıldı");

            // Her oyuncuya kendi taşlarını gönder
            foreach (var player in validPlayers)
            {
                var gameState = new GameState
                {
                    YourStones = playerStones[player.Id],
                    IndicatorStone = indicatorStone,
                    OkeyStone = okeyStone,
                    RemainingStoneCount = remainingStones.Count,
                    CurrentPlayer = validPlayers[0].Id, // İlk oyuncu başlar
                    IsGameStarted = true,
                    CurrentTurn = game.CurrentTurn,
                    IsHugoTurn = game.IsHugoTurn,
                    Position = player.Position,
                    TotalPlayers = validPlayers.Count
                };

                await Clients.Client(player.Id).SendAsync("GameStarted", gameState);
                _logger.LogInformation($"Oyun durumu {player.Name} oyuncusuna gönderildi");
            }

            // Tüm oyunculara güncel durumu bildir
            await NotifyGameState(game);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Oyun başlatılırken hata oluştu: {ex}");
            throw new HubException($"Oyun başlatılırken hata oluştu: {ex.Message}");
        }
    }

    private List<Stone> CreateAndShuffleStones()
    {
        var stones = new List<Stone>();
        
        // Normal taşları oluştur (her renk ve sayıdan 2'şer adet)
        foreach (StoneColor color in Enum.GetValues(typeof(StoneColor)))
        {
            for (var number = 1; number <= 13; number++)
            {
                for (var duplicate = 0; duplicate < 2; duplicate++)
                {
                    stones.Add(new Stone(number, color));
                }
            }
        }

        // Joker taşlarını ekle
        stones.Add(new Stone(0, StoneColor.Black, true));
        stones.Add(new Stone(0, StoneColor.Black, true));

        // Taşları karıştır
        var random = new Random();
        return stones.OrderBy(x => random.Next()).ToList();
    }

    private Stone DetermineOkeyStone(Stone indicatorStone)
    {
        if (indicatorStone.IsJoker)
        {
            // Gösterge joker çıkarsa, o tur Hugo turu olur
            return new Stone(0, StoneColor.Black, true) { IsOkey = true };
        }

        var nextNumber = indicatorStone.Number == 13 ? 1 : indicatorStone.Number + 1;
        return new Stone(nextNumber, indicatorStone.Color) { IsOkey = true };
    }

    private async Task NotifyGameState(Game game)
    {
        _logger.LogInformation($"Oyun durumu güncelleniyor - GameId: {game.Id}, Toplam Oyuncu: {game.Players.Count}");
        
        // Boş isimli oyuncuları filtrele
        var validPlayers = game.Players.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
        _logger.LogInformation($"Geçerli oyuncu sayısı: {validPlayers.Count}");

        // Tüm oyuncuların bilgilerini hazırla
        var allPlayerInfo = validPlayers
            .OrderBy(p => p.Position)
            .Select(p => new {
                p.Name,
                p.Position,
                StoneCount = p.Stones.Count,
                LastThrownStone = p.LastThrownStone
            })
            .ToList();

        _logger.LogInformation($"Toplam oyuncu bilgisi: {string.Join(", ", allPlayerInfo.Select(p => $"{p.Name}({p.Position})"))}");

        // Her oyuncuya güncel durumu gönder
        foreach (var player in validPlayers)
        {
            _logger.LogInformation($"Oyuncu {player.Name} için oyun durumu güncelleniyor - Pozisyon: {player.Position}");

            var playerState = new
            {
                Position = player.Position,
                CurrentPlayer = game.CurrentPlayerId == player.Id,
                YourStones = player.Stones,
                OtherPlayers = allPlayerInfo,
                OpenedPers = game.OpenedPers,
                OkeyStone = game.OkeyStone,
                CurrentTurn = game.CurrentTurn,
                IsHugoTurn = game.IsHugoTurn,
                TotalPlayers = validPlayers.Count
            };

            await Clients.Client(player.Id).SendAsync("GameStateUpdated", playerState);
        }
    }

    public async Task<object> GetGameState(string gameId)
    {
        _logger.LogInformation($"Game state istendi - GameId: {gameId}");
        
        var game = _gameService.GetGame(gameId);
        if (game == null)
        {
            _logger.LogWarning($"Oyun bulunamadı - GameId: {gameId}");
            throw new HubException("Oyun bulunamadı.");
        }

        // Boş isimli oyuncuları filtrele
        var validPlayers = game.Players.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
        _logger.LogInformation($"Geçerli oyuncu sayısı: {validPlayers.Count}");

        // Tüm oyuncuların bilgilerini hazırla
        var allPlayerInfo = validPlayers
            .OrderBy(p => p.Position)
            .Select(p => new {
                p.Name,
                p.Position,
                StoneCount = p.Stones.Count,
                LastThrownStone = p.LastThrownStone
            })
            .ToList();

        var currentPlayer = validPlayers.FirstOrDefault(p => p.Id == Context.ConnectionId);
        if (currentPlayer == null)
        {
            _logger.LogWarning($"Mevcut oyuncu bulunamadı - ConnectionId: {Context.ConnectionId}");
            throw new HubException("Oyuncu bulunamadı.");
        }

        var gameState = new
        {
            Position = currentPlayer.Position,
            CurrentPlayer = game.CurrentPlayerId == currentPlayer.Id,
            Stones = currentPlayer.Stones,
            Players = allPlayerInfo,
            OpenedPers = game.OpenedPers,
            OkeyStone = game.OkeyStone,
            CurrentTurn = game.CurrentTurn,
            IsHugoTurn = game.IsHugoTurn,
            TotalPlayers = validPlayers.Count,
            IsGameStarted = game.Status == GameStatus.InProgress
        };

        _logger.LogInformation($"Game state gönderiliyor - Oyuncu: {currentPlayer.Name}, Pozisyon: {currentPlayer.Position}");
        return gameState;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_userGameMap.TryRemove(Context.ConnectionId, out var gameId) && 
            _games.TryGetValue(gameId, out var game))
        {
            var player = game.Players.FirstOrDefault(p => p.Id == Context.ConnectionId);
            if (player != null)
            {
                game.Players.Remove(player);
                await Clients.Group(gameId).SendAsync("PlayerLeft", player.Name);

                if (game.Players.Count == 0)
                {
                    _games.TryRemove(gameId, out _);
                }
            }
        }

        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }
} 