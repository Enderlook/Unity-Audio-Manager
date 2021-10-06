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
        public float Volume {
            get {
                if (handle == null || generation != handle.Generation)
                    ThrowAudioHasEndedException();
                return handle.Volume;
            }
            set {
                if (value < 0 || value > 1)
                    ThrowVolumeArgumentException();
                if (handle == null || generation != handle.Generation)
                    ThrowAudioHasEndedException();
                handle.Volume = value;

#if UNITY_2020_2_OR_NEWER
                static
#endif
                void ThrowVolumeArgumentException() => throw new ArgumentException("value", "Must be a value from 0 to 1.");
            }
        }

        /// <summary>
        /// Determines if this instance is default.
        /// </summary>
        public bool IsDefault => generation == GENERATION_DEFAULT;

        /// <summary>
        /// Determines if audio is being played.
        /// </summary>
        public bool IsPlaying {
            get {
                if (handle == null)
                {
                    ThrowIfDefault();
                    return false;
                }
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
            if (handle == null)
                ThrowIfDefault();

            if (generation == handle.Generation)
            {
                memento = handle.Stop();
                generation = GENERATION_STOPPED;
            }
            else if (generation == GENERATION_PAUSED)
                generation = GENERATION_STOPPED;
            else
                ThrowCanNotStop();

#if UNITY_2020_2_OR_NEWER
            static
#endif
            void ThrowCanNotStop() => throw new InvalidOperationException("Can't stop an audio which is already stopped or finallized.");
        }

        /// <summary>
        /// Pause the execution of an audio.
        /// </summary>
        /// <returns><see langword="true"/> if the audio was paused or has been paused. <see langword="false"/> if the audio has already finalized.</returns>
        /// <exception cref="ArgumentException">Throw when instance is default.</exception>
        /// <exception cref="InvalidOperationException">Throw when audio is stopped, paused or has finalized.</exception>
        public void Pause()
        {
            if (handle == null)
                SlowPath(ref this);

            if (generation == handle.Generation)
            {
                memento = handle.Pause();
                generation = GENERATION_PAUSED;
                return;
            }

            ThrowCanNotPauseException();

#if UNITY_2020_2_OR_NEWER
            static
#endif
            void SlowPath(ref AudioPlay self)
            {
                self.ThrowIfDefault();
                ThrowCanNotPauseException();
            }

#if UNITY_2020_2_OR_NEWER
            static
#endif
            void ThrowCanNotPauseException() => throw new InvalidOperationException("Can't pause an audio which is stopped, paused or has finallized.");
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
            if (handle == null)
            {
                SlowPath(ref this);
                return;
            }

            int state = generation;
            if (state == handle.Generation)
                ThrowCanNotPlayException();

            handle = Pool.GetOrCreateHandle();
            generation = handle.Generation;
            if (state == GENERATION_PAUSED)
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

            //[MethodImpl(MethodImplOptions.NoInlining)] // TODO: Add On C# 9
#if UNITY_2020_2_OR_NEWER
            static
#endif
            void SlowPath(ref AudioPlay self)
            {
                self.ThrowIfDefault();

                self.handle = Pool.GetOrCreateHandle();
                self.generation = self.handle.Generation;

                self.memento = self.memento.FromZero();
                self.handle.LoadMementoAndPlay(self.memento);
            }
        }

        private void ThrowAudioHasEndedException()
        {
            ThrowIfDefault();
            throw new InvalidOperationException("Audio has already ended.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDefault()
        {
            if (generation == GENERATION_DEFAULT)
                ThrowIsDefaultException();

#if UNITY_2020_2_OR_NEWER
            static
#endif
            void ThrowIsDefaultException() => throw new ArgumentException("Instance is default.");
        }
    }
}