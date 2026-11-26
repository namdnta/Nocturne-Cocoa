using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.Data
{
    /// <summary>
    /// List of every character in the game, with lookup by ID. Used by systems that only know an ID
    /// (save files, the Character Journal, customers in M6).
    /// </summary>
    [CreateAssetMenu(menuName = "Nocturne Cocoa/Characters/Character Database", fileName = "CharacterDatabase")]
    public class CharacterDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<CharacterProfileSO> characters = new List<CharacterProfileSO>();

        private Dictionary<string, CharacterProfileSO> lookup;

        public IReadOnlyList<CharacterProfileSO> Characters
        {
            get { return characters; }
        }

        public CharacterProfileSO Find(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return null;
            }

            if (lookup == null)
            {
                BuildLookup();
            }

            CharacterProfileSO profile;
            return lookup.TryGetValue(characterId, out profile) ? profile : null;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, CharacterProfileSO>();
            foreach (CharacterProfileSO profile in characters)
            {
                if (profile == null || string.IsNullOrEmpty(profile.CharacterId))
                {
                    continue;
                }

                if (lookup.ContainsKey(profile.CharacterId))
                {
                    Debug.LogWarning($"[CharacterDatabase] Duplicate character ID '{profile.CharacterId}'.", this);
                    continue;
                }

                lookup.Add(profile.CharacterId, profile);
            }
        }

        private void OnEnable()
        {
            lookup = null;
        }

        private void OnValidate()
        {
            lookup = null;
        }

#if UNITY_EDITOR
        public void EditorSetCharacters(List<CharacterProfileSO> list)
        {
            characters = list;
            lookup = null;
        }
#endif
    }
}