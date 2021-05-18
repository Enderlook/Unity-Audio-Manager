using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// Represent a collection of audios where only one of them (random) is played when request.
    /// </summary>
    [CreateAssetMenu(fileName = "Audio Bag", menuName = "Enderlook/Audio Manager/Audio Bag")]
    public sealed class AudioBag : AudioFile
    {
        [SerializeField, Tooltip("Audio files of this bag.\nOne of them is randomly chosen when play is requested.")]
        private AudioFile[] files;

        internal override void ConfigureAudioSource(AudioSource audioSource)
            => files[Random.Range(0, files.Length)].ConfigureAudioSource(audioSource);
    }
}