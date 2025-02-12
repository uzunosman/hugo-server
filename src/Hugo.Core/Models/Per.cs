using System.Collections.Generic;
using System.Linq;

namespace Hugo.Core.Models;

public class Per
{
    public int Id { get; set; }
    public List<Stone> Stones { get; private set; }
    public PerType Type { get; private set; }
    public string OwnerId { get; set; }

    public Per(List<Stone> stones, string ownerId)
    {
        Stones = stones;
        OwnerId = ownerId;
        DeterminePerType();
    }

    private void DeterminePerType()
    {
        if (IsSameNumber())
            Type = PerType.SameNumber;
        else if (IsSequential())
            Type = PerType.Sequential;
        else
            Type = PerType.Invalid;
    }

    private bool IsSameNumber()
    {
        var nonJokerStones = Stones.Where(s => !s.IsJoker).ToList();
        return nonJokerStones.All(s => s.Number == nonJokerStones[0].Number);
    }

    private bool IsSequential()
    {
        var nonJokerStones = Stones.Where(s => !s.IsJoker).OrderBy(s => s.Number).ToList();
        var firstColor = nonJokerStones[0].Color;
        
        if (!nonJokerStones.All(s => s.Color == firstColor))
            return false;

        for (int i = 1; i < nonJokerStones.Count; i++)
        {
            if (nonJokerStones[i].Number != nonJokerStones[i - 1].Number + 1)
                return false;
        }
        return true;
    }

    public int CalculateValue()
    {
        return Stones.Sum(s => s.Value);
    }

    public bool CanAddStone(Stone stone)
    {
        if (Type == PerType.Invalid)
            return false;

        var tempStones = new List<Stone>(Stones) { stone };
        var tempPer = new Per(tempStones, OwnerId);
        
        return tempPer.Type == Type;
    }
}

public enum PerType
{
    Invalid,
    SameNumber,  // Aynı sayılı farklı renk
    Sequential   // Aynı renk ardışık sayı
} 