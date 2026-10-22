using System;
using System.Globalization;
using System.Linq;

namespace SnapLocalGameLab.Core
{
    internal sealed class LocalGameOptions
    {
        public string League = "VsBalanceAi";
        public string AiPreset;
        public string PlayerDeck = "";
        public string AiDeck = "";
        public DeckEntry[] PlayerCards;
        public DeckEntry[] AiCards;
        public string[] Locations;
        public int? Seed;
        public byte[] RandomKey;
        public PresentationOptions Presentation = new PresentationOptions();
        public bool SkipNewCardPrompts = true;
        public bool EnableSnapping = true;
        public int StartingCubes = 1;
        public int MaximumCubes = 8;
        public int SnapsPerPlayer = 1;
        public bool DoubleFinalTurn = true;

        public static byte[] ParseRandomKey(string text)
        {
            var hex = new string((text ?? "").Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
            if (hex.Length != 32) throw new ArgumentException("The random key must contain 32 hexadecimal digits (16 bytes).");
            var key = new byte[16];
            for (var i = 0; i < key.Length; i++)
                if (!byte.TryParse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out key[i]))
                    throw new ArgumentException("The random key must contain only hexadecimal digits: 0–9 and A–F.");
            return key;
        }

        public static void ValidateDeck(DeckEntry[] cards)
        {
            if (cards == null) return;
            if (cards.Length == 0 || cards.Any(c => c == null || string.IsNullOrWhiteSpace(c.CardId) || c.Count < 1 || c.Count > 99))
                throw new ArgumentException("A manual deck needs at least one card, with 1–99 copies per selection.");
            if (cards.Sum(c => (long)c.Count) > 99)
                throw new ArgumentException("A manual deck can contain at most 99 cards.");
            if (cards.Select(c => c.CardId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != cards.Length)
                throw new ArgumentException("Set the count for each card instead of adding it twice.");
        }

        public void ValidateSnapping()
        {
            ValidateDeck(PlayerCards);
            ValidateDeck(AiCards);
            if (Seed.HasValue && RandomKey != null)
                throw new ArgumentException("Choose either an integer seed or a random key.");
            if (RandomKey != null && RandomKey.Length != 16)
                throw new ArgumentException("The random key must be exactly 16 bytes.");
            if (StartingCubes < 1 || MaximumCubes < StartingCubes || MaximumCubes > 1000000)
                throw new ArgumentException("Cubes must be between 1 and 1,000,000, with maximum at least the starting value.");
            if (EnableSnapping && MaximumCubes == StartingCubes)
                throw new ArgumentException("Maximum cubes must exceed starting cubes when snapping is enabled.");
            if (SnapsPerPlayer < 1 || SnapsPerPlayer > 4)
                throw new ArgumentException("Snaps per player must be between 1 and 4.");
        }
    }

    internal sealed class DeckEntry
    {
        public string CardId;
        public int Count;
    }

    internal sealed class PresentationOptions
    {
        public string PlayerName;
        public string OpponentName;
        public AvatarChoice PlayerAvatar;
        public AvatarChoice OpponentAvatar;
    }
}
