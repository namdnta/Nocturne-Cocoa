using System;
using System.Collections;
using NocturneCocoa.Core;
using TMPro;
using UnityEngine;

namespace NocturneCocoa.UI
{
    /// <summary>
    /// Reveals a TextMeshPro text one character at a time, with short pauses after punctuation and an
    /// optional voice "blip". Rich text tags (&lt;b&gt;, &lt;color&gt;) are supported because TMP counts
    /// only visible characters.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public class TypewriterEffect : MonoBehaviour
    {
        [Tooltip("Characters per second. 0 = show instantly.")]
        [SerializeField, Min(0f)] private float charactersPerSecond = 40f;

        [Tooltip("Extra pause after . , ! ? and similar.")]
        [SerializeField, Min(0f)] private float punctuationPause = 0.12f;

        [Header("Voice blip (optional)")]
        [Tooltip("Play the blip every N visible characters.")]
        [SerializeField, Min(1)] private int blipEveryCharacters = 2;
        [SerializeField, Range(0f, 1f)] private float blipVolume = 0.5f;
        [SerializeField, Range(0f, 0.3f)] private float blipPitchVariation = 0.06f;

        /// <summary>The whole text is now visible (typed out or skipped).</summary>
        public event Action Completed;

        private TMP_Text text;
        private Coroutine typingRoutine;
        private AudioClip blip;
        private float blipPitch = 1f;
        private AudioManager audioManager;

        public bool IsTyping { get; private set; }

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            Services.TryGet(out audioManager);
        }

        /// <summary>Starts typing the text. Pass a voice clip (e.g. CharacterProfileSO.VoiceBlip) or null.</summary>
        public void Play(string content, AudioClip voiceBlip = null, float voicePitch = 1f)
        {
            Stop();

            blip = voiceBlip;
            blipPitch = voicePitch;
            text.text = content;
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();

            if (charactersPerSecond <= 0f || !isActiveAndEnabled)
            {
                Finish();
                return;
            }

            typingRoutine = StartCoroutine(TypeRoutine());
        }

        /// <summary>Show the rest of the text immediately.</summary>
        public void Complete()
        {
            if (IsTyping)
            {
                Stop();
                Finish();
            }
        }

        public void Clear()
        {
            Stop();
            text.text = string.Empty;
        }

        private void Stop()
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
                typingRoutine = null;
            }

            IsTyping = false;
        }

        private IEnumerator TypeRoutine()
        {
            IsTyping = true;

            int total = text.textInfo.characterCount;
            int visible = 0;
            float secondsPerCharacter = 1f / charactersPerSecond;
            float timer = 0f;

            while (visible < total)
            {
                timer += Time.unscaledDeltaTime;

                while (timer >= secondsPerCharacter && visible < total)
                {
                    timer -= secondsPerCharacter;
                    visible++;
                    text.maxVisibleCharacters = visible;

                    char character = text.textInfo.characterInfo[visible - 1].character;
                    PlayBlip(visible, character);

                    if (IsPunctuation(character) && visible < total)
                    {
                        timer = -punctuationPause;
                        break;
                    }
                }

                yield return null;
            }

            typingRoutine = null;
            Finish();
        }

        private void Finish()
        {
            IsTyping = false;
            text.maxVisibleCharacters = 99999;
            if (Completed != null) Completed.Invoke();
        }

        private void PlayBlip(int visibleCount, char character)
        {
            if (blip == null || audioManager == null || char.IsWhiteSpace(character) || visibleCount % blipEveryCharacters != 0)
            {
                return;
            }

            float pitch = blipPitch + UnityEngine.Random.Range(-blipPitchVariation, blipPitchVariation);
            audioManager.PlaySfx(blip, blipVolume, pitch);
        }

        private static bool IsPunctuation(char c)
        {
            return c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':' || c == '…';
        }
    }
}