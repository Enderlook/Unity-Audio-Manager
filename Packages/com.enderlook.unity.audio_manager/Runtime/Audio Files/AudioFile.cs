using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// Represent an audio file.
    /// </summary>
    public abstract class AudioFile : ScriptableObject
    {
        /// <summary>
        /// Configure the audio file to start executing this file.
        /// </summary>
        /// <param name="audioSource">Audio source to configure.</param>
        /// <param name="loop">Whenever audio must loop.</param>
        /// <returns>Enumerator of the sound.</returns>
        internal abstract IAudioFileNextEnumerator StartEnumerator(AudioSource audioSource, bool loop);
    }
}