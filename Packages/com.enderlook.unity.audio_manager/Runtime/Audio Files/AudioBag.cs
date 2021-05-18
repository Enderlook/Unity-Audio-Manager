using System;
using System.Runtime.CompilerServices;

using UnityEngine;

using Random = UnityEngine.Random;

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

        internal override IAudioFileNextEnumerator StartEnumerator(AudioSource audioSource, bool loop)
        {
            AudioFile audioFile;
            try
            {
                audioFile = files[Random.Range(0, files.Length)];
            }
            catch (IndexOutOfRangeException)
            {
                ThrowIsEmptyException();
                audioFile = null;
            }
            IAudioFileNextEnumerator currentEnumerator = audioFile.StartEnumerator(audioSource, false);

            if (loop)
                return new LoopEnumerator(this, currentEnumerator);
            else
                return currentEnumerator;
        }

        private sealed class LoopEnumerator : IAudioFileNextEnumerator
        {
            private AudioBag bag;
            private IAudioFileNextEnumerator currentEnumerator;

            public LoopEnumerator(AudioBag bag, IAudioFileNextEnumerator currentEnumerator)
            {
                this.bag = bag;
                this.currentEnumerator = currentEnumerator;
            }

            public void ApplyCurrent(AudioSource audioSource) => currentEnumerator.ApplyCurrent(audioSource);

            public bool MoveNext(AudioSource source)
            {
                if (currentEnumerator.MoveNext(source))
                    return true;

                AudioFile[] files = bag.files;
                AudioFile audioFile;
                try
                {
                    audioFile = files[Random.Range(0, files.Length)];
                }
                catch (IndexOutOfRangeException)
                {
                    ThrowIsEmptyException();
                    audioFile = null;
                }
                currentEnumerator = audioFile.StartEnumerator(source, false);

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIsEmptyException() => throw new InvalidOperationException("Audio bag is empty.");
    }
}