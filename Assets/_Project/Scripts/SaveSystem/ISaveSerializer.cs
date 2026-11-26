namespace NocturneCocoa.SaveSystem
{
    /// <summary>
    /// Converts SaveData to and from text. Kept behind an interface so the format can change
    /// later (e.g. Newtonsoft JSON for dictionaries, or encryption) without touching SaveManager.
    /// </summary>
    public interface ISaveSerializer
    {
        string Serialize(SaveData data);

        /// <summary>Returns null if the text is not valid save data.</summary>
        SaveData Deserialize(string text);
    }
}
