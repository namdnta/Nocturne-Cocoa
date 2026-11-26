using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.Core
{
    /// <summary>
    /// Plays background music (with crossfade) and sound effects, and owns the volume settings.
    /// Volume settings are per-device preferences, so they live in PlayerPrefs, not in save slots.
    ///
    /// M1 scope: AudioClip based. An AudioMixer, ambience layer and "audio cue" ScriptableObjects
    /// can be added in M7 without changing callers.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        private const string MasterVolumeKey = "NocturneCocoa.Audio.Master";
        private const string MusicVolumeKey = "NocturneCocoa.Audio.Music";
        private const string SfxVolumeKey = "NocturneCocoa.Audio.Sfx";

        [Header("Default volumes (used until the player changes them)")]
        [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;

        [Header("Sources")]
        [Tooltip("How many sound effects can play with different pitch at the same time.")]
        [SerializeField, Min(1)] private int sfxSourceCount = 8;

        private readonly AudioSource[] musicSources = new AudioSource[2];
        private readonly List<AudioSource> sfxSources = new List<AudioSource>();
        private int activeMusicIndex;
        private int nextSfxIndex;
        private Coroutine musicFadeRoutine;

        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        public AudioClip CurrentMusic
        {
            get
            {
                AudioSource active = musicSources[activeMusicIndex];
                return active != null && active.isPlaying ? active.clip : null;
            }
        }

        private float MusicOutputVolume
        {
            get { return MasterVolume * MusicVolume; }
        }

        private float SfxOutputVolume
        {
            get { return MasterVolume * SfxVolume; }
        }

        /// <summary>Called by BootstrapLoader.</summary>
        public void Initialize()
        {
            for (int i = 0; i < musicSources.Length; i++)
            {
                musicSources[i] = CreateSource($"Music {i + 1}", true);
            }

            for (int i = 0; i < sfxSourceCount; i++)
            {
                sfxSources.Add(CreateSource($"SFX {i + 1}", false));
            }

            MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
            ApplyVolumes();
        }

        // ---------------------------------------------------------------- Music

        public void PlayMusic(AudioClip clip, float fadeSeconds = 1f)
        {
            if (clip == null)
            {
                StopMusic(fadeSeconds);
                return;
            }

            if (CurrentMusic == clip)
            {
                return;
            }

            AudioSource from = musicSources[activeMusicIndex];
            activeMusicIndex = 1 - activeMusicIndex;
            AudioSource to = musicSources[activeMusicIndex];

            to.clip = clip;
            to.volume = 0f;
            to.Play();

            StartFade(from, to, fadeSeconds);
        }

        public void StopMusic(float fadeSeconds = 1f)
        {
            StartFade(musicSources[activeMusicIndex], null, fadeSeconds);
        }

        // ---------------------------------------------------------------- SFX

        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null || sfxSources.Count == 0)
            {
                return;
            }

            AudioSource source = sfxSources[nextSfxIndex];
            nextSfxIndex = (nextSfxIndex + 1) % sfxSources.Count;

            source.pitch = pitch;
            source.volume = SfxOutputVolume;
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        // ---------------------------------------------------------------- Volume settings

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            ApplyVolumes();
        }

        // ---------------------------------------------------------------- Internals

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            GameObject child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D sound
            return source;
        }

        private void ApplyVolumes()
        {
            // While a crossfade runs it controls the music volumes itself.
            if (musicFadeRoutine == null && musicSources[activeMusicIndex] != null)
            {
                musicSources[activeMusicIndex].volume = MusicOutputVolume;
            }

            for (int i = 0; i < sfxSources.Count; i++)
            {
                sfxSources[i].volume = SfxOutputVolume;
            }
        }

        private void StartFade(AudioSource from, AudioSource to, float seconds)
        {
            if (musicFadeRoutine != null)
            {
                StopCoroutine(musicFadeRoutine);
                musicFadeRoutine = null;
            }

            // If a previous fade was interrupted, silence any source that isn't part of this fade.
            for (int i = 0; i < musicSources.Length; i++)
            {
                AudioSource source = musicSources[i];
                if (source != null && source != from && source != to)
                {
                    source.Stop();
                    source.volume = 0f;
                }
            }

            musicFadeRoutine = StartCoroutine(FadeRoutine(from, to, Mathf.Max(0f, seconds)));
        }

        private IEnumerator FadeRoutine(AudioSource from, AudioSource to, float seconds)
        {
            float fromStart = from != null ? from.volume : 0f;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);

                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, t);
                if (to != null) to.volume = Mathf.Lerp(0f, MusicOutputVolume, t);
                yield return null;
            }

            if (from != null)
            {
                from.Stop();
                from.volume = 0f;
            }

            if (to != null)
            {
                to.volume = MusicOutputVolume;
            }

            musicFadeRoutine = null;
        }
    }
}