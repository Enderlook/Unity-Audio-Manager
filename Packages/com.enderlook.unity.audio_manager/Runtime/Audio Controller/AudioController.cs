using System;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Audio;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// Global controller of the audio manager.
    /// </summary>
    public sealed class AudioController : MonoBehaviour
    {
#if UNITY_EDITOR
        private const bool HideController = true;
        internal const bool HidePooledObjects = true;
#endif

        private static AudioControllerUnit configuration;
        private static AudioController behaviour;

        private static AudioControllerUnit Instance {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (configuration == null)
                    GetConfiguration();
                return configuration;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void GetConfiguration()
        {
            AudioControllerUnit[] controllers = Resources.LoadAll<AudioControllerUnit>("");
            if (controllers.Length != 1)
            {
                if (controllers.Length > 1)
                    throw new InvalidOperationException("Multiple instances of " + nameof(AudioControllerUnit) + " were found in the Resources folder.");
                else if (controllers.Length == 0)
                    throw new InvalidOperationException("No instance of " + nameof(AudioControllerUnit) + " was found in the Resources folder.");
            }
            configuration = controllers[0];
            behaviour = new GameObject().AddComponent<AudioController>();
        }

        /// <summary>
        /// Determines if the game is muted.
        /// </summary>
        public static bool MasterVolumeMuted {
            get => Instance.masterVolumeMuted;
            set => Instance.masterVolumeMuted = value;
        }

        /// <summary>
        /// Global modifier of all audios.
        /// </summary>
        public static float MasterVolume {
            get => Instance.masterVolume;
            set => Instance.masterVolume = value;
        }

        /// <summary>
        /// Global audio mixter group.
        /// </summary>
        public static AudioMixerGroup MasterAudioMixer => Instance.masterAudioMixer;

        /// <summary>
        /// Global audio mixter group.
        /// </summary>
        public static bool MusicVolumeMuted {
            get => Instance.musicVolumeMuted;
            set => Instance.musicVolumeMuted = value;
        }

        /// <summary>
        /// Global modifier of all musics.
        /// </summary>
        public static float MusicVolume {
            get => Instance.musicVolume;
            set => Instance.musicVolume = value;
        }

        /// <summary>
        /// Music audio mixter group.
        /// </summary>
        public static AudioMixerGroup MusicAudioMixer => Instance.musicAudioMixer;

        /// <summary>
        /// Determines if sounds are muted.
        /// </summary>
        public static bool SoundVolumeMuted {
            get => Instance.soundVolumeMuted;
            set => Instance.soundVolumeMuted = value;
        }

        /// <summary>
        /// Global modifier of all sounds.
        /// </summary>
        public static float SoundVolume {
            get => Instance.soundVolume;
            set => Instance.soundVolume = value;
        }

        /// <summary>
        /// Sound audio mixter group.
        /// </summary>
        public static AudioMixerGroup SoundAudioMixer => Instance.soundAudioMixer;

        internal static void SetBasicToAudioSource(AudioSource audioSource,  AudioType audioType)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    audioSource.outputAudioMixerGroup = MusicAudioMixer;
                    break;
                case AudioType.Sound:
                    audioSource.outputAudioMixerGroup = SoundAudioMixer;
                    break;
                default:
                    Debug.Assert(false, "Impossible state.");
                    break;
            }
        }

        /// <summary>
        /// Play an audio once on the specified location.
        /// </summary>
        /// <param name="audioFile">Audio to play.</param>
        /// <param name="location">Location when audio is played.</param>
        public static AudioPlay PlayOneShoot(AudioFile audioFile, Vector3 location = default)
        {
            if (audioFile == null)
                ThrowNullArgumentExceptionAudioFile();

            return AudioPlay.Play(audioFile, location, false);
        }

        /// <summary>
        /// Play an audio once following the specified transform position.
        /// </summary>
        /// <param name="audioFile">Audio to play.</param>
        /// <param name="transform">Transform to follow when audio is played.</param>
        public static AudioPlay PlayOneShoot(AudioFile audioFile, Transform transform = default)
        {
            if (audioFile == null)
                ThrowNullArgumentExceptionAudioFile();

            return AudioPlay.Play(audioFile, transform, false);
        }

        /// <summary>
        /// Play a looped audio on the specified location.
        /// </summary>
        /// <param name="audioFile">Audio to play.</param>
        /// <param name="location">Location when audio is played.</param>
        public static AudioPlay PlayLoop(AudioFile audioFile, Vector3 location = default)
        {
            if (audioFile == null)
                ThrowNullArgumentExceptionAudioFile();

            return AudioPlay.Play(audioFile, location, true);
        }

        /// <summary>
        /// Play a looped audio following the specified transform position.
        /// </summary>
        /// <param name="audioFile">Audio to play.</param>
        /// <param name="transform">Transform to follow when audio is played.</param>
        public static AudioPlay PlayLoop(AudioFile audioFile, Transform transform = default)
        {
            if (audioFile == null)
                ThrowNullArgumentExceptionAudioFile();

            return AudioPlay.Play(audioFile, transform, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNullArgumentExceptionAudioFile() => throw new ArgumentNullException("audioUnit");

#if UNITY_EDITOR
        private static bool isExiting;
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize2()
        {
            isExiting = false;
            UnityEditor.EditorApplication.playModeStateChanged +=
                (UnityEditor.PlayModeStateChange playModeState) => isExiting = playModeState == UnityEditor.PlayModeStateChange.ExitingPlayMode;
        }
#endif

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void Awake()
        {
            if (behaviour != null)
            {
                Destroy(gameObject);
                throw new InvalidOperationException("An instance of a " + nameof(AudioController) + " already exists. Only one can exists.");
            }

            behaviour = this;
            gameObject.name = "Enderlook.Unity.AudioManager.AudioController";
            DontDestroyOnLoad(gameObject);
            Instance.UpdateValues();

#if UNITY_EDITOR
            if (HideController)
                gameObject.hideFlags = HideFlags.HideAndDontSave;
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void Update()
        {
            Instance.UpdateValues();
            AudioPlay.TryClearPool();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void FixedUpdate() => Instance.UpdateValues();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void OnDisable()
        {
#if UNITY_EDITOR
            if (isExiting)
                return;
#endif

            throw new InvalidOperationException(nameof(AudioController) + " should not be disabled. State has been corrupted.");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (isExiting)
                return;
#endif

            throw new InvalidOperationException(nameof(AudioController) + " should not be disabled. State has been corrupted.");
        }
    }
}