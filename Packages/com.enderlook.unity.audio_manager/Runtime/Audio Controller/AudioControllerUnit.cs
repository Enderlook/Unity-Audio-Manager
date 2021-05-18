using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Audio;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// Represent the global settings of the audio manager.
    /// </summary>
    [CreateAssetMenu(fileName = "Global Audio Controller Configuration", menuName = "Enderlook/Audio Manager/Audio Controller Unit")]
    internal sealed class AudioControllerUnit : ScriptableObject
    {
        [Header("Master")]
        [SerializeField, Tooltip("Determines if the game is muted.")]
        internal bool masterVolumeMuted;

        [SerializeField, Range(0, 1), Tooltip("Global modifier of all audios.")]
        internal float masterVolume = 1;

        [SerializeField, Tooltip("Global audio mixter group.")]
        internal AudioMixerGroup masterAudioMixer;

        [SerializeField, Tooltip("Name of the variable in the Master Audio Mixer that controls its volume.")]
        private string masterVolumeName;

        [Header("Music")]
        [SerializeField, Tooltip("Determines if music is muted.")]
        internal bool musicVolumeMuted;

        [SerializeField, Range(0, 1), Tooltip("Global modifier of all musics.")]
        internal float musicVolume = 1;

        [SerializeField, Tooltip("Music audio mixter group.")]
        internal AudioMixerGroup musicAudioMixer;

        [SerializeField, Tooltip("Name of the variable in the Music Audio Mixer that controls its volume.")]
        private string musicVolumeName;

        [Header("Sound")]
        [SerializeField, Tooltip("Determines if sounds are muted.")]
        internal bool soundVolumeMuted;

        [SerializeField, Range(0, 1), Tooltip("Global modifier of all sounds.")]
        internal float soundVolume = 1;

        [SerializeField, Tooltip("Sound audio mixter group.")]
        internal AudioMixerGroup soundAudioMixer;

        [SerializeField, Tooltip("Name of the variable in the Sound Audio Mixer that controls its volume.")]
        private string soundVolumeName;

        [Header("Others")]
        [SerializeField, Tooltip("Amount of Audio Sources that will be pooled.")]
        internal int audioSourcePoolSize = 100;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateValues()
        {
            masterAudioMixer.audioMixer.SetFloat(masterVolumeName, GetVolume(masterVolumeMuted, masterVolume));
            soundAudioMixer.audioMixer.SetFloat(soundVolumeName, GetVolume(soundVolumeMuted, soundVolume));
            musicAudioMixer.audioMixer.SetFloat(musicVolumeName, GetVolume(musicVolumeMuted, musicVolume));
            AudioPlay.PoolSize = audioSourcePoolSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetVolume(bool isMuted, float volume) => isMuted ? 0 : (volume == 0 ? 0 : Mathf.Log(volume) * 20);
    }
}