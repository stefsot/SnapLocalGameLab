using System;

namespace SnapLocalGameLab.Core
{
    internal sealed class LocalGameOptions
    {
        public string League = "VsBalanceAi";
        public string PlayerDeck = "";
        public string AiDeck = "";
        public string Locations = "";
        public int? Seed;
        public bool SkipNewCardPrompts = true;
        public bool EnableSnapping = true;
        public int StartingCubes = 1;
        public int MaximumCubes = 8;
        public int SnapsPerPlayer = 1;
        public bool DoubleFinalTurn = true;

        public void ValidateSnapping()
        {
            if (StartingCubes < 1 || MaximumCubes < StartingCubes || MaximumCubes > 1000000)
                throw new ArgumentException("Cubes must be between 1 and 1,000,000, with maximum at least the starting value.");
            if (EnableSnapping && MaximumCubes == StartingCubes)
                throw new ArgumentException("Maximum cubes must exceed starting cubes when snapping is enabled.");
            if (SnapsPerPlayer < 1 || SnapsPerPlayer > 4)
                throw new ArgumentException("Snaps per player must be between 1 and 4.");
        }
    }
}
