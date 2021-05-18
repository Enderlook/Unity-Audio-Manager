using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// Iterator to play multiple sounds.<br/>
    /// The first sound is got from <see cref="AudioFile.StartEnumerator(AudioSource)"/>, subsequent sounds are got from <see cref="MoveNext(AudioSource)"/>.
    /// </summary>
    internal interface IAudioFileNextEnumerator
    {
        /// <summary>
        /// Configures the <paramref name="audioSource"/> to play the next sound, if any.
        /// </summary>
        /// <param name="audioSource">Audio source to configure.</param>
        /// <returns>On <see langword="false"/>, the enumation has ended.</returns>
        bool MoveNext(AudioSource audioSource);

        /// <summary>
        /// Applies current audio configuration to <paramref name="audioSource"/>.
        /// </summary>
        /// <param name="audioSource">Audio source to configure.</param>
        void ApplyCurrent(AudioSource audioSource);
    }
}