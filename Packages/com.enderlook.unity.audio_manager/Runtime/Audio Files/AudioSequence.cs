using System;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// Represent a collection of audios where all of them are played when request in sequential order.
    /// </summary>
    [CreateAssetMenu(fileName = "Audio Sequence", menuName = "Enderlook/Audio Manager/Audio Sequence")]
    public sealed class AudioSequence : AudioFile
    {
        [SerializeField, Tooltip("Audio files of this collection.\nAll of them will be played in sequential order when is requested.")]
        private AudioFile[] files;

        internal override IAudioFileNextEnumerator StartEnumerator(AudioSource audioSource, bool loop)
        {
            AudioFile audioFile;
            try
            {
                audioFile = files[0];
            }
            catch (IndexOutOfRangeException)
            {
                ThrowIsEmptyException();
                audioFile = null;
            }

            IAudioFileNextEnumerator currentEnumerator = audioFile.StartEnumerator(audioSource, false);
            return loop ? (IAudioFileNextEnumerator)new LoopEnumerator(this, currentEnumerator) : (IAudioFileNextEnumerator)new Enumerator(this, currentEnumerator);
        }

        private static void ThrowIsEmptyException() => throw new InvalidOperationException("Audio collection is empty.");

        private sealed class Enumerator : IAudioFileNextEnumerator
        {
            private AudioSequence sequence;
            private int index;
            private IAudioFileNextEnumerator currentEnumerator;

            public Enumerator(AudioSequence sequence, IAudioFileNextEnumerator currentEnumerator)
            {
                this.sequence = sequence;
                this.currentEnumerator = currentEnumerator;
                index = 1;
            }

            public void ApplyCurrent(AudioSource audioSource) => currentEnumerator.ApplyCurrent(audioSource);

            public bool MoveNext(AudioSource source)
            {
                if (currentEnumerator.MoveNext(source))
                    return true;

                AudioFile[] files = sequence.files;
                if (index < files.Length)
                {
                    AudioFile file = files[index++];
                    currentEnumerator = file.StartEnumerator(source, false);
                    return true;
                }

                return false;
            }
        }

        private sealed class LoopEnumerator : IAudioFileNextEnumerator
        {
            private AudioSequence sequence;
            private int index;
            private IAudioFileNextEnumerator currentEnumerator;

            public LoopEnumerator(AudioSequence sequence, IAudioFileNextEnumerator currentEnumerator)
            {
                this.sequence = sequence;
                this.currentEnumerator = currentEnumerator;
                index = 1;
            }

            public void ApplyCurrent(AudioSource audioSource) => currentEnumerator.ApplyCurrent(audioSource);

            public bool MoveNext(AudioSource source)
            {
                if (currentEnumerator.MoveNext(source))
                    return true;

                AudioFile[] files = sequence.files;
                if (index < files.Length)
                {
                    AudioFile file = files[index++];
                    currentEnumerator = file.StartEnumerator(source,false);
                    return true;
                }
                else
                    return MoveNextRare(source, files);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private bool MoveNextRare(AudioSource source, AudioFile[] files)
            {
                index = 0;
                AudioFile file;
                try
                {
                    file = files[index];
                }
                catch (IndexOutOfRangeException)
                {
                    ThrowIsEmptyException();
                    file = null;
                }
                currentEnumerator = file.StartEnumerator(source, false);
                return true;
            }
        }
    }
}