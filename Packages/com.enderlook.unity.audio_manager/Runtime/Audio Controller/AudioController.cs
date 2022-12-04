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

        internal static AudioControllerUnit Instance {
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
            GameObject gameObject = new GameObject();
#if UNITY_EDITOR
            if (HideController)
                gameObject.hideFlags = HideFlags.HideAndDontSave;
#endif
            behaviour = gameObject.AddComponent<AudioController>();
        }

        /// <summary>
        /// Determines if the game is muted.
        /// </summary>
        public static bool MasterIsMuted {
            get => Instance.GetAudioGroup(0).isMuted;
            set => Instance.GetAudioGroup(0).isMuted = value;
        }

        /// <summary>
        /// Global modifier of all audios.
        /// </summary>
        public static float MasterVolume {
            get => Instance.GetAudioGroup(0).volume;
            set {
                if (value > 1 || value < 0)
                    ThrowVolumeOutOfRangeException();
                Instance.GetAudioGroup(0).volume = value;
            }
        }

        /// <summary>
        /// Global audio mixter group.
        /// </summary>
        public static AudioMixerGroup MasterAudioMixerGroup => Instance.GetAudioGroup(0).audioMixerGroup;

        /// <summary>
        /// Determines if an audio group is muted.
        /// </summary>
        /// <param name="audioGroupName">Name of the audio group.</param>
        /// <returns>Whenever the specifeid audio group is muted or not.</returns>
        public static bool GetMuted(string audioGroupName)
            => Instance.GetAudioGroup(audioGroupName).isMuted;

        /// <summary>
        /// Determines if an audio group is muted.
        /// </summary>
        /// <param name="audioGroupName">Name of the audio group.</param>
        /// <param name="isMuted">Whenever the specifeid audio group is muted or not.</param>
        public static bool SetMuted(string audioGroupName, bool isMuted)
            => Instance.GetAudioGroup(audioGroupName).isMuted = isMuted;

        /// <summary>
        /// Get the volume of an audio group.
        /// </summary>
        /// <param name="audioGroupName">Name of the audio group.</param>
        /// <returns>Volume of the specifeid audio group.</returns>
        public static float GetVolume(string audioGroupName)
            => Instance.GetAudioGroup(audioGroupName).volume;

        /// <summary>
        /// Set the volume of an audio group.
        /// </summary>
        /// <param name="audioGroupName">Name of the audio group.</param>
        /// <param name="volume">Volume of the specifeid audio group.</param>
        public static void SetVolume(string audioGroupName, float volume)
            => Instance.GetAudioGroup(audioGroupName).volume = volume;

        /// <summary>
        /// Get the audio mixer group of an audio group.
        /// </summary>
        /// <param name="audioGroupName">Name of the audio group.</param>
        /// <returns>Audio mixer group of the specifeid audio group.</returns>
        public static AudioMixerGroup GetAudioMixerGroup(string audioGroupName)
            => Instance.GetAudioGroup(audioGroupName).audioMixerGroup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetBasicToAudioSource(AudioSource audioSource, string audioGroup)
            => audioSource.outputAudioMixerGroup = GetAudioMixerGroup(audioGroup);

        internal static string[] GetGroupNamesEditorOnly() => Instance.GetGroupNamesEditorOnly();

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

        private static void ThrowNullArgumentExceptionAudioFile() => throw new ArgumentNullException("audioUnit");

        private static void ThrowVolumeOutOfRangeException() => throw new ArgumentOutOfRangeException("value", "Must be a value from 0 to 1.");

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