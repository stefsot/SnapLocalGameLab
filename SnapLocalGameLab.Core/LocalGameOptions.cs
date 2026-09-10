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
    }
}
