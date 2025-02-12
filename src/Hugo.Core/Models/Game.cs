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
    public GameState State { get; private set; }
    public bool IsHugoTurn => CurrentTurn == 1 || CurrentTurn == 5 || CurrentTurn == 9;

    public Game(string id, List<Player> players)
    {
        Id = id;
        Players = players;
        Deck = new List<Stone>();
        OpenedPers = new List<Per>();
        CurrentTurn = 1;
        State = GameState.WaitingToStart;
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
        Deck.Add(new Stone(0, StoneColor.Black, true));
        Deck.Add(new Stone(0, StoneColor.Black, true));

        // Taşları karıştır
        Random rnd = new Random();
        Deck = Deck.OrderBy(x => rnd.Next()).ToList();
    }

    public void StartGame()
    {
        if (Players.Count != 4)
            throw new InvalidOperationException("Oyun 4 oyuncu ile başlamalıdır.");

        State = GameState.InProgress;
        DealInitialStones();
        DetermineOkeyStone();
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
        if (IsHugoTurn)
        {
            // Hugo turlarında joker taşları okey olur
            var jokers = Deck.Where(s => s.IsJoker).ToList();
            foreach (var joker in jokers)
            {
                joker.IsOkey = true;
            }
            OkeyStone = jokers.First();
        }
        else
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
        State = GameState.Finished;
        // Final puanlarını hesapla
        foreach (var player in Players)
        {
            if (!player.HasOpenedHand)
                player.Score += 400; // El açmama cezası
            else
                player.Score += player.CalculateHandValue();
        }
    }
}

public enum GameState
{
    WaitingToStart,
    InProgress,
    Finished
} 