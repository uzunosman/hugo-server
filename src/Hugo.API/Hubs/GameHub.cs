using Microsoft.AspNetCore.SignalR;
using Hugo.Core.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Hugo.API.Hubs;

public class GameHub : Hub
{
    private static readonly ConcurrentDictionary<string, Game> _games = new();
    private static readonly ConcurrentDictionary<string, string> _userGameMap = new();
    private readonly ILogger<GameHub> _logger;

    public GameHub(ILogger<GameHub> logger)
    {
        _logger = logger;
    }

    public async Task CreateGame(string playerName)
    {
        var player = new Player(Context.ConnectionId, playerName);
        var game = new Game(Guid.NewGuid().ToString(), new List<Player> { player });
        
        _games.TryAdd(game.Id, game);
        _userGameMap.TryAdd(Context.ConnectionId, game.Id);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);
        await Clients.Caller.SendAsync("GameCreated", game.Id);
    }

    public async Task JoinGame(string gameId, string playerName)
    {
        if (!_games.TryGetValue(gameId, out var game))
        {
            await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
            return;
        }

        if (game.Players.Count >= 4)
        {
            await Clients.Caller.SendAsync("Error", "Oyun dolu.");
            return;
        }

        var player = new Player(Context.ConnectionId, playerName);
        game.Players.Add(player);
        _userGameMap.TryAdd(Context.ConnectionId, gameId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).SendAsync("PlayerJoined", player);

        if (game.Players.Count == 4)
        {
            game.StartGame();
            await NotifyGameState(game);
        }
    }

    public async Task DrawStone()
    {
        if (!_userGameMap.TryGetValue(Context.ConnectionId, out var gameId) || 
            !_games.TryGetValue(gameId, out var game))
        {
            await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
            return;
        }

        if (game.CurrentPlayerId != Context.ConnectionId)
        {
            await Clients.Caller.SendAsync("Error", "Sıra sizde değil.");
            return;
        }

        try
        {
            var stone = game.DrawStone();
            var player = game.Players.First(p => p.Id == Context.ConnectionId);
            player.AddStone(stone);

            await Clients.Caller.SendAsync("StoneDrawn", stone);
            await Clients.Group(gameId).SendAsync("PlayerDrewStone", player.Name);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ThrowStone(Stone stone)
    {
        if (!_userGameMap.TryGetValue(Context.ConnectionId, out var gameId) || 
            !_games.TryGetValue(gameId, out var game))
        {
            await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
            return;
        }

        if (game.CurrentPlayerId != Context.ConnectionId)
        {
            await Clients.Caller.SendAsync("Error", "Sıra sizde değil.");
            return;
        }

        var player = game.Players.First(p => p.Id == Context.ConnectionId);
        if (player.RemoveStone(stone))
        {
            game.NextTurn();
            await Clients.Group(gameId).SendAsync("StoneThrown", stone, player.Name);
            await NotifyGameState(game);
        }
    }

    public async Task OpenPer(List<Stone> stones)
    {
        if (!_userGameMap.TryGetValue(Context.ConnectionId, out var gameId) || 
            !_games.TryGetValue(gameId, out var game))
        {
            await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
            return;
        }

        var player = game.Players.First(p => p.Id == Context.ConnectionId);
        var per = new Per(stones, player.Id);

        if (per.CalculateValue() < 51)
        {
            await Clients.Caller.SendAsync("Error", "Per değeri en az 51 olmalıdır.");
            return;
        }

        foreach (var stone in stones)
        {
            player.RemoveStone(stone);
        }

        game.OpenedPers.Add(per);
        player.HasOpenedHand = true;

        await Clients.Group(gameId).SendAsync("PerOpened", per, player.Name);
    }

    public async Task AddStoneToPer(Stone stone, int perId)
    {
        if (!_userGameMap.TryGetValue(Context.ConnectionId, out var gameId) || 
            !_games.TryGetValue(gameId, out var game))
        {
            await Clients.Caller.SendAsync("Error", "Oyun bulunamadı.");
            return;
        }

        var per = game.OpenedPers.FirstOrDefault(p => p.Id == perId);
        if (per == null)
        {
            await Clients.Caller.SendAsync("Error", "Per bulunamadı.");
            return;
        }

        if (!per.CanAddStone(stone))
        {
            await Clients.Caller.SendAsync("Error", "Bu taş bu pere eklenemez.");
            return;
        }

        var player = game.Players.First(p => p.Id == Context.ConnectionId);
        if (player.RemoveStone(stone))
        {
            per.AddStone(stone);
            await Clients.Group(gameId).SendAsync("StoneAddedToPer", stone, perId, player.Name);
        }
    }

    private async Task NotifyGameState(Game game)
    {
        foreach (var player in game.Players)
        {
            var playerState = new
            {
                CurrentPlayer = game.CurrentPlayerId == player.Id,
                YourStones = player.Stones,
                OtherPlayers = game.Players.Where(p => p.Id != player.Id)
                    .Select(p => new { p.Name, StoneCount = p.Stones.Count }),
                OpenedPers = game.OpenedPers,
                OkeyStone = game.OkeyStone,
                CurrentTurn = game.CurrentTurn,
                IsHugoTurn = game.IsHugoTurn
            };

            await Clients.Client(player.Id).SendAsync("GameStateUpdated", playerState);
        }
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