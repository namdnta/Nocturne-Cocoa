namespace NocturneCocoa.SceneManagement
{
    /// <summary>
    /// Scene names in one place, so a typo is a compile error instead of a runtime failure.
    /// These must match the scene file names and be listed in Build Profiles > Scene List.
    /// </summary>
    public static class SceneNames
    {
        public const string Bootstrap = "Bootstrap";
        public const string CafeExploration = "Cafe_Exploration";
        public const string Dialogue = "Dialogue_Scene";
        public const string Cooking = "Cooking_Scene";

        /// <summary>Scenes the player can be in during play (everything except Bootstrap).</summary>
        public static readonly string[] Gameplay = { CafeExploration, Dialogue, Cooking };
    }
}