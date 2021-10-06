using System;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// A handle that represent the playing of an <see cref="AudioFile"/>.<br/>
    /// This handle should not be copied. It should be treated with move semantics.
    /// </summary>
    public partial struct AudioPlay
    {
        private Handle handle;
        private int generation; // If negative or 0 this contains other information described by constants named GENERATION_.
        private Memento memento;

        private const int GENERATION_PAUSED = -1;
        private const int GENERATION_STOPPED = -2;
        private const int GENERATION_DEFAULT = 0;

        /// <summary>
        /// Configures the relative volume of the audio source.
        /// </summary>
        /// <exception cref="ArgumentException">Throw when instance is default.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Throw when value is out of range 0-1.</exception>
        public float Volume {
            get {
                int generation = this.generation;
                if (handle == null || generation != handle.Generation)
                {
                    if (generation == GENERATION_DEFAULT)
                        ThrowIsDefaultException();
                    return memento.manualVolume;
                }
                return handle.Volume;
            }
            set {
                if (value < 0 || value > 1)
                    ThrowVolumeArgumentException();
                int generation = this.generation;
                if (handle == null || generation != handle.Generation)
                {
                    if (generation == GENERATION_DEFAULT)
                        ThrowIsDefaultException();
                    memento = memento.WithVolume(value);
                }
                else
                    handle.Volume = value;

#if UNITY_2020_2_OR_NEWER
                static
#endif
                void ThrowVolumeArgumentException() => throw new ArgumentException("value", "Must be a value from 0 to 1.");
            }
        }

        /// <summary>
        /// Determines if this instance is default, i.e: was constructed from <c>default(AudioPlay)</c> or <c>new AudioPlay()</c>.
        /// </summary>
        public bool IsDefault => generation == GENERATION_DEFAULT;

        /// <summary>
        /// Determines if audio is being played.
        /// </summary>
        /// <exception cref="ArgumentException">Throw when instance is default.</exception>
        public bool IsPlaying {
            get {
                int generation = this.generation;
                if (generation == GENERATION_DEFAULT)
                    ThrowIsDefaultException();
                return generation == handle.Generation;
            }
        }

        /// <summary>
        /// Determines if audio is paused.
        /// </summary>
        public bool IsPaused => generation == GENERATION_PAUSED;

        /// <summary>
        /// Determines if audio is stopped, which is not the same as if the audio has finallized.
        /// </summary>
        public bool IsStopped => generation == GENERATION_STOPPED;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AudioPlay Play(AudioFile audioFile, Vector3 location, bool loop = false)
            => new AudioPlay(audioFile, location, loop);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AudioPlay Play(AudioFile audioFile, Transform follow, bool loop = false)
            => new AudioPlay(audioFile, follow, loop);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AudioPlay(AudioFile audioFile, Vector3 position, bool loop = false)
        {
            handle = Pool.GetOrCreateHandle();
            generation = handle.Generation;
            handle.Feed(audioFile, loop);
            handle.Play();
            handle.SetPosition(position);
            memento = handle.SaveMemento();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AudioPlay(AudioFile audioFile, Transform follow, bool loop = false)
        {
            handle = Pool.GetOrCreateHandle();
            generation = handle.Generation;
            handle.Feed(audioFile, loop);
            handle.Play();
            handle.FollowTransform(follow);
            memento = handle.SaveMemento();
        }

        /// <summary>
        /// Stop the execution of an audio. If the audio was paused, its duration is reseted.
        /// </summary>
        /// <exception cref="ArgumentException">Throw when instance is default.</exception>
        /// <exception cref="InvalidOperationException">Thrown when audio was already stopped or finalized.</exception>
        public void Stop()
        {
            int generation_ = generation;
            if (handle != null)
            {
                if (generation_ == handle.Generation)
                {
                    memento = handle.Stop();
                    generation = GENERATION_STOPPED;
                    return;
                }
                else if (generation_ == GENERATION_PAUSED)
                {
                    generation = GENERATION_STOPPED;
                    return;
                }
            }
            ThrowHelper(generation_);

#if UNITY_2020_2_OR_NEWER
            static
#endif
            void ThrowHelper(int generation)
            {
                if (generation == GENERATION_DEFAULT)
                    ThrowIsDefaultException();
                if (generation == GENERATION_STOPPED)
                    throw new InvalidOperationException("Can't stop an audio which is stopped.");
                throw new InvalidOperationException("Can't stop an audio which has already finalized.");
            }
        }

        /// <summary>
        /// Pause the execution of an audio.
        /// </summary>
        /// <returns><see langword="true"/> if the audio was paused or has been paused. <see langword="false"/> if the audio has already finalized.</returns>
        /// <exception cref="ArgumentException">Throw when instance is default.</exception>
        /// <exception cref="InvalidOperationException">Throw when audio is stopped, paused or has finalized.</exception>
        public void Pause()
        {
            int generation_ = generation;
            if (handle != null && generation_ == handle.Generation)
            {
                memento = handle.Pause();
                generation = GENERATION_PAUSED;
            }
            else
                ThrowHelper(generation_);

#if UNITY_2020_2_OR_NEWER
            static
#endif
            void ThrowHelper(int generation)
            {
                if (generation == GENERATION_DEFAULT)
                    ThrowIsDefaultException();
                if (generation == GENERATION_PAUSED)
                    throw new InvalidOperationException("Can't stop an audio which is paused.");
                if (generation == GENERATION_STOPPED)
                    throw new InvalidOperationException("Can't stop an audio which is stopped.");
                throw new InvalidOperationException("Can't stop an audio which has already finalized.");
            }
        }

        /// <summary>
        /// Reanude the execution of an audio if <see cref="Pause"/> was executed.
        /// Start from zero if has <see cref="Stop"/> was executed.
        /// Start from zero if audio has finalized.
        /// </summary>
        /// <exception cref="ArgumentException">Throw when instance is default.</exception>
        /// <exception cref="InvalidOperationException">Throw when audio is already playing.</exception>
        public void Play()
        {
            int generation_ = generation;
            if (handle == null)
            {
                if (generation_ == GENERATION_DEFAULT)
                    ThrowIsDefaultException();
            }
            else if (generation_ == handle.Generation)
                ThrowCanNotPlayException();

            handle = Pool.GetOrCreateHandle();
            generation = handle.Generation;
            if (generation_ == GENERATION_PAUSED)
                handle.LoadMementoAndPlay(memento);
            else
            {
                memento = memento.FromZero();
                handle.LoadMementoAndPlay(memento);
            }

#if UNITY_2020_2_OR_NEWER
            static
#endif
            void ThrowCanNotPlayException() => throw new InvalidOperationException("Can't play an audio which is already playing.");
        }

        private static void ThrowIsDefaultException() => throw new ArgumentException("Instance is default.");
    }
}