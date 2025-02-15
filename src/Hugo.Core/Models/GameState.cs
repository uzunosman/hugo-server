using System.Collections.Generic;

namespace Hugo.Core.Models;

public class GameState
{
    public List<Stone> YourStones { get; set; } = new();
    public Stone? IndicatorStone { get; set; }
    public Stone? OkeyStone { get; set; }
    public int RemainingStoneCount { get; set; }
    public string? CurrentPlayer { get; set; }
    public bool IsGameStarted { get; set; }
    public int CurrentTurn { get; set; }
    public bool IsHugoTurn { get; set; }
    public List<Player> OtherPlayers { get; set; } = new();
    public int Position { get; set; }
} 