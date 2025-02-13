using System.Collections.Generic;
using System.Linq;

namespace Hugo.Core.Models;

public class Player
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<Stone> Stones { get; private set; }
    public int Score { get; set; }
    public bool HasOpenedHand { get; set; }
    public int Position { get; set; } // Masadaki pozisyonu (0-3)
    public Stone? LastThrownStone { get; set; } // Son attığı taş

    public Player(string id, string name)
    {
        Id = id;
        Name = name;
        Stones = new List<Stone>();
        Score = 0;
        HasOpenedHand = false;
        Position = -1; // Başlangıçta pozisyon atanmamış
    }

    public void AddStone(Stone stone)
    {
        Stones.Add(stone);
    }

    public bool RemoveStone(Stone stone)
    {
        return Stones.Remove(stone);
    }

    public void ClearStones()
    {
        Stones.Clear();
        HasOpenedHand = false;
    }

    public int CalculateHandValue()
    {
        int value = 0;
        foreach (var stone in Stones)
        {
            value += stone.Value;
            if (stone.IsOkey)
                value += 100; // Kullanılmayan okey cezası
        }
        return value;
    }

    public int GetTotalStoneValue()
    {
        return Stones.Sum(s => s.Value);
    }
} 