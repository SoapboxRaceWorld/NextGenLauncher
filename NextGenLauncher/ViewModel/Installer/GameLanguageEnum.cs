namespace NextGenLauncher.ViewModel.Installer
{
    public enum GameLanguageEnum
    {
        English,
        German,
        Spanish,
        Russian,
        Polish,
        French,
        Portuguese,
        SimplifiedChinese,
        TraditionalChinese
    }

    public class GameLanguageOption
    {
        public GameLanguageEnum Language { get; }
        public string DisplayName { get; }
        public string PackageKey { get; }
        public string GameKey { get; }

        public GameLanguageOption(GameLanguageEnum language, string displayName, string packageKey, string gameKey)
        {
            Language = language;
            DisplayName = displayName;
            PackageKey = packageKey;
            GameKey = gameKey;
        }
    }
}