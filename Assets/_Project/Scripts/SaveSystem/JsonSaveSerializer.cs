using System;
using UnityEngine;

namespace NocturneCocoa.SaveSystem
{
    /// <summary>JSON save format using Unity's built-in JsonUtility (no extra packages).</summary>
    public class JsonSaveSerializer : ISaveSerializer
    {
        private readonly bool prettyPrint;

        public JsonSaveSerializer(bool prettyPrint)
        {
            this.prettyPrint = prettyPrint;
        }

        public string Serialize(SaveData data)
        {
            return JsonUtility.ToJson(data, prettyPrint);
        }

        public SaveData Deserialize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<SaveData>(text);
            }
            catch (ArgumentException)
            {
                // Corrupt or truncated JSON.
                return null;
            }
        }
    }
}
