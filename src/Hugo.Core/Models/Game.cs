using System;
using System.Collections.Generic;
using System.Linq;

namespace Hugo.Core.Models;

public class Game
{
    public string Id { get; }
    public List<Player> Players { get; }
    public List<Stone> Deck { get; private set; }
    public List<Per> OpenedPers { get; }
    public Stone? OkeyStone { get; private set; }
    public int CurrentTurn { get; private set; }
    public string? CurrentPlayerId { get; private set; }
    public GameStatus Status { get; private set; }
    public bool IsHugoTurn => CurrentTurn == 1 || CurrentTurn == 5 || CurrentTurn == 9;

    public Game(string id, List<Player> players)
    {
        Id = id;
        Players = players;
        Deck = new List<Stone>();
        OpenedPers = new List<Per>();
        CurrentTurn = 1;
        Status = GameStatus.WaitingToStart;
        InitializeDeck();
    }

    private void InitializeDeck()
    {
        // Her renk için 1-13 arası sayılar (her sayıdan 2 adet)
        foreach (StoneColor color in Enum.GetValues(typeof(StoneColor)))
        {
            for (int number = 1; number <= 13; number++)
            {
                Deck.Add(new Stone(number, color));
                Deck.Add(new Stone(number, color));
            }
        }

        // 2 adet joker taşı
        var joker1 = new Stone(0, StoneColor.Black, true);
        var joker2 = new Stone(0, StoneColor.Black, true);
        
        // Hugo turlarında jokerler okey olur
        if (IsHugoTurn)
        {
            joker1.IsOkey = true;
            joker2.IsOkey = true;
            OkeyStone = joker1;
        }
        
        Deck.Add(joker1);
        Deck.Add(joker2);

        // Taşları karıştır
        Random rnd = new Random();
        Deck = Deck.OrderBy(x => rnd.Next()).ToList();
    }

    public void StartGame()
    {
        if (Players.Count != 4)
            throw new InvalidOperationException("Oyun 4 oyuncu ile başlatılmalıdır.");

        Status = GameStatus.InProgress;
        
        // Desteyi oluştur ve karıştır (sadece başlangıçta)
        Deck.Clear();
        InitializeDeck();
        
        // Okey taşını belirle
        if (!IsHugoTurn)
        {
            DetermineOkeyStone();
        }
        
        // Taşları dağıt
        DealInitialStones();
        
        // İlk oyuncuyu belirle
        CurrentPlayerId = Players[0].Id;
    }

    private void DealInitialStones()
    {
        // İlk oyuncuya 15 taş, diğerlerine 14'er taş
        for (int i = 0; i < 15; i++)
        {
            if (i == 14)
            {
                Players[0].AddStone(DrawStone());
                break;
            }

            foreach (var player in Players)
            {
                player.AddStone(DrawStone());
            }
        }
    }

    private void DetermineOkeyStone()
    {
        // Normal turlarda gösterge taşının bir üstü okey olur
        var indicator = DrawStone();
        int okeyNumber = indicator.Number == 13 ? 1 : indicator.Number + 1;
        OkeyStone = new Stone(okeyNumber, indicator.Color);

        // Okey taşlarını işaretle
        var okeyStones = Deck.Where(s => s.Number == okeyNumber && s.Color == indicator.Color).ToList();
        foreach (var stone in okeyStones)
        {
            stone.IsOkey = true;
        }
    }

    public Stone DrawStone()
    {
        if (Deck.Count == 0)
            throw new InvalidOperationException("Deste boş");

        var stone = Deck[0];
        Deck.RemoveAt(0);
        return stone;
    }

    public void NextTurn()
    {
        var currentPlayerIndex = Players.FindIndex(p => p.Id == CurrentPlayerId);
        currentPlayerIndex = (currentPlayerIndex + 1) % Players.Count;
        CurrentPlayerId = Players[currentPlayerIndex].Id;

        if (currentPlayerIndex == 0)
        {
            CurrentTurn++;
            if (CurrentTurn > 9)
            {
                EndGame();
            }
        }
    }

    private void EndGame()
    {
        Status = GameStatus.Finished;
        // Final puanlarını hesapla
        foreach (var player in Players)
        {
            if (!player.HasOpenedHand)
                player.Score += 400; // El açmama cezası
            else
                player.Score += player.CalculateHandValue();
        }
    }

    public void Start(Dictionary<string, List<Stone>> playerStones, Stone indicatorStone, Stone okeyStone, List<Stone> remainingStones)
    {
        if (Status != GameStatus.WaitingToStart)
            throw new InvalidOperationException("Oyun zaten başlatılmış.");

        // Oyunculara taşlarını dağıt
        foreach (var player in Players)
        {
            if (playerStones.TryGetValue(player.Id, out var stones))
            {
                player.Stones.Clear();
                player.Stones.AddRange(stones);
            }
        }

        // Okey taşını ayarla
        OkeyStone = okeyStone;
        
        // Desteyi güncelle
        Deck = remainingStones;
        
        // Oyunu başlat
        Status = GameStatus.InProgress;
        CurrentTurn = 1;
        CurrentPlayerId = Players[0].Id;
    }
}

public enum GameStatus
{
    WaitingToStart,
    InProgress,
    Finished
} 