﻿using System;
using System.Linq;
using System.Collections.Generic;

namespace LostCities
{
    // TODO: Disallow drawing a card that was just discarded
    // TODO: enforce increasing value when investing
    // TODO: draw "playing area"
    // TODO: show unplayable cards in dimmed color

    class Program
    {
        static void Main(string[] args)
        {
            var game = new Game();

            while (game.Deck.Count > 0)
            {
                TakeTurn(game.Player1);
                if (game.Deck.Count > 0)
                    TakeTurn(game.Player2);
            }

            Console.WriteLine("=======================");
            Console.WriteLine($"Player 1 scored {game.Player1.Score}");
            Console.WriteLine($"Player 2 scored {game.Player2.Score}");
        }

        static void TakeTurn(Player player)
        {
            WriteCards($"Player {player.Number}", player.Hand);
            var index = ReadInt("Pick a card to discard");
            Console.WriteLine($"What do you want to do with the {player.Hand[index]}?");
            var choice = ReadOptions("[I]nvest or [D]iscard?", 'i', 'd');
            switch (choice)
            {
                case 'i':
                    player.Invest(index);
                    break;
                case 'd':
                    player.Discard(index);
                    break;
            }

            Console.WriteLine($"From where will you draw a card?");
            var choices = GetChoices(player.Game.Discards);
            choice = ReadOptions(choices.prompt, choices.choices);
            switch (choice)
            {
                case 'd':
                    player.DrawFrom(player.Game.Deck);
                    break;
                case 'r':
                    player.DrawFrom(player.Game.Discards[Suit.Red]);
                    break;
                case 'g':
                    player.DrawFrom(player.Game.Discards[Suit.Green]);
                    break;
                case 'w':
                    player.DrawFrom(player.Game.Discards[Suit.White]);
                    break;
                case 'b':
                    player.DrawFrom(player.Game.Discards[Suit.Blue]);
                    break;
                case 'y':
                    player.DrawFrom(player.Game.Discards[Suit.Yellow]);
                    break;
            }

            Console.WriteLine($"Player {player.Number} Score is {player.Score}");

            (string prompt, char[] choices) GetChoices(IEnumerable<KeyValuePair<Suit, IList<Card>>> discards)
            {
                var piles = discards.Where(d=>d.Value.Any()).Select(d=>d.Key.ToString()).Prepend("Deck");
                var options = piles.Select(p=>p.ToLowerInvariant()[0]).ToArray();
                var prompt = string.Join(", ", piles.Select(p=>p.Insert(0, "[").Insert(2, "]")));
                return (prompt, options);
            }
        }

        static char ReadOptions(string prompt, params char[] options)
        {
            Console.WriteLine(prompt);
            try
            {
                do
                {
                    var key = Console.ReadKey();
                    if (options.Contains(key.KeyChar))
                        return key.KeyChar;
                }while(true);
            }
            finally
            {
                Console.WriteLine();
            }
        }

        static int ReadInt(string prompt)
        {
            Console.WriteLine(prompt);
            try
            {
                do
                {
                    var key = Console.ReadKey();
                    if (char.IsDigit(key.KeyChar))
                        return int.Parse(key.KeyChar.ToString());
                }while(true);
            }
            finally
            {
                Console.WriteLine();
            }
        }

        static void WriteCards(string title, IEnumerable<Card> cards)
        {
            Console.WriteLine("====================");
            Console.WriteLine(title);
            Console.WriteLine("--------------------");
            var i = 0;
            foreach (var c in cards)
            {
                Console.Write($"{i++}. ");
                Console.ForegroundColor = GetConsoleColor(c.Suit);
                Console.WriteLine(c);
                Console.ResetColor();
            }
        }

        static ConsoleColor GetConsoleColor(Suit suit)
        {
            switch (suit)
            {
                case Suit.Blue: return ConsoleColor.Blue;
                case Suit.Green: return ConsoleColor.Green;
                case Suit.Red: return ConsoleColor.Red;
                case Suit.White: return ConsoleColor.White;
                case Suit.Yellow: return ConsoleColor.Yellow;
                default: throw new InvalidOperationException("Unknown suit");
            }
        }
    }

    public enum Suit
    {
        Red,
        Green,
        White,
        Blue,
        Yellow
    }

    public class Card
    {
        public Card(int value, Suit suit)
        {
            this.IsMultiplier = value < 2;
            this.Value = IsMultiplier ? 0 : value;
            this.Suit = suit;
        }

        public Suit Suit {get;}
        public int Value{get;}
        public bool IsMultiplier {get;}

        public override string ToString()
        {
            if (IsMultiplier)
                return $"{Suit} Investment";
            else
                return $"{Suit} {Value}";
        }
    }

    public class Adventure
    {
        public Adventure(Suit suit)
        {
            this.Suit = suit;
            this.investments = new List<Card>();
        }

        private readonly List<Card> investments;

        public Suit Suit { get; }
        public IReadOnlyList<Card> Investments => investments;
        
        public int Cost => investments.Any(c=>!c.IsMultiplier) ? 20 : 20 * Multiplier;

        public int Value => investments.Any() ? -Cost + investments.Sum(c => c.Value) * Multiplier : 0;

        public int Multiplier => investments.Count(c => c.IsMultiplier) + 1;

        public void Invest(Card card)
        {
            this.investments.Add(card);
        }
    }

    public class Player
    {
        public Player(Game game, int number, List<Card> hand)
        {
            this.hand = hand;
            this.Number = number;
            this.Adventures = Enum.GetValues(typeof(Suit)).OfType<Suit>().ToDictionary(s=>s, s=>new Adventure(s));
            this.Game = game;
        }

        public int Number {get;}

        private readonly List<Card> hand;

        public IReadOnlyList<Card> Hand => hand;
        
        public IDictionary<Suit, Adventure> Adventures { get; }
        
        public int Score => Adventures.Values.Sum(a => a.Value);

        public Game Game {get;}

        public Player Invest(int index)
        {
            var card = this.hand[index];
            this.hand.RemoveAt(index);
            this.Adventures[card.Suit].Invest(card);
            return this;
        }

        public Player Discard(int index)
        {
            var card = this.hand[index];
            this.hand.RemoveAt(index);
            this.Game.Discard(card);
            return this;
        }

        public void DrawFrom(IList<Card> deck)
        {
            var card = deck.Last();
            deck.Remove(card);
            this.hand.Add(card);
        }
    }

    public class Game
    {
        public static IList<Card> GenerateDeck()
        {
            return (from suit in Enum.GetValues(typeof(Suit)).OfType<Suit>()
                    from v in Enumerable.Range(-1, 12)
                    select new Card(v, suit)).ToList();
        }

        public Game()
        {
            this.Deck = GenerateDeck().Shuffle();
            this.Discards = Enum.GetValues(typeof(Suit)).OfType<Suit>().ToDictionary(s=>s, s => (IList<Card>)new List<Card>());
            var hands = Deal(8, 2);
            this.Player1 = new Player(this, 1, hands[0].ToList());
            this.Player2 = new Player(this, 2, hands[1].ToList());

            Card[][] Deal(int numberOfCards, int numberOfPlayers)
            {
                var result = new Card[numberOfPlayers][];
                for (var p = 0; p < numberOfPlayers; p++)
                    result[p] = new Card[numberOfCards];

                for (var i = 0; i<numberOfCards; i++)
                {
                    for (var p = 0; p < numberOfPlayers; p++)
                    {
                        var card = this.Deck[0];
                        this.Deck.RemoveAt(0);
                        result[p][i] = card;
                    }
                }

                return result;
            }
        }

        public IList<Card> Deck {get;}
        public IReadOnlyDictionary<Suit, IList<Card>> Discards {get;}
        public Player Player1 {get;}
        public Player Player2 {get;}

        public void Discard(Card card)
        {
            this.Discards[card.Suit].Add(card);
        }
    }

    public static class Extensions
    {
        private static Random rng = new Random();  

        public static IList<T> Shuffle<T>(this IList<T> list)  
        {  
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  

            return list;
        }
    }
}
