namespace Hugo.Core.Models;

public class Stone
{
    public int Number { get; set; }  // 1-13 arası sayı
    public StoneColor Color { get; set; }
    public bool IsJoker { get; set; }
    public bool IsOkey { get; set; }

    public int Value => Number * 10;  // Taşın puan değeri

    public Stone(int number, StoneColor color, bool isJoker = false)
    {
        Number = number;
        Color = color;
        IsJoker = isJoker;
        IsOkey = false;
    }

    public override string ToString()
    {
        if (IsJoker)
            return "Joker";
        return $"{Color} {Number}";
    }
}

public enum StoneColor
{
    Red,
    Yellow,
    Blue,
    Black
} 