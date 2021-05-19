using System;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// A handle that represent the playing of an <see cref="AudioFile"/>.
    /// </summary>
    public partial struct AudioPlay
    {
        private Handle handle;
        private int generation; // If negative this contains other information described by constants named GENERATION_.
        private Memento memento;

        private const int GENERATION_PAUSED = -1;
        private const int GENERATION_STOPPED = -2;

        /// <summary>
        /// Configures the relative volume of the audio source.
        /// </summary>
        public float Volume {
            get {
                if (generation != handle.Generation)
                    ThrowAudioHasEnded();
                return handle.Volume;
            }
            set {
                if (value < 0 || value > 1)
                    ThrowVolumeArgumentException();
                if (generation != handle.Generation)
                    ThrowAudioHasEnded();
                handle.Volume = value;
            }
        }

        /// <summary>
        /// Determines if audio is being played.
        /// </summary>
        public bool IsPlaying => generation == handle.Generation;

        /// <summary>
        /// Determines if audio is paused.
        /// </summary>
        public bool IsPaused => generation == GENERATION_PAUSED;

        /// <summary>
        /// Determines if audio is stopped.
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
            handle.TrackPosition(position);
            memento = handle.SaveMemento();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AudioPlay(AudioFile audioFile, Transform follow, bool loop = false)
        {
            handle = Pool.GetOrCreateHandle();
            generation = handle.Generation;
            handle.Feed(audioFile, loop);
            handle.Play();
            handle.TrackPosition(follow);
            memento = handle.SaveMemento();
        }

        /// <summary>
        /// Stop the execution of an audio.
        /// </summary>
        public void Stop()
        {
            if (generation == handle.Generation)
                memento = handle.Stop();
            generation = GENERATION_STOPPED;
        }

        /// <summary>
        /// Pause the execution of an audio.
        /// </summary>
        /// <returns><see langword="true"/> if the audio was paused or has been paused. <see langword="false"/> if the audio has already finalized.</returns>
        public void Pause()
        {
            if (generation == handle.Generation)
            {
                memento = handle.Pause();
                generation = GENERATION_PAUSED;
            }
            else
                ThrowCanNotPause();
        }

        /// <summary>
        /// Reanude the execution of an audio from <see cref="Pause"/> or start from zero if has finalized or stopped.
        /// </summary>
        public void Reanude()
        {
            if (generation != handle.Generation)
            {
                handle = Pool.GetOrCreateHandle();
                int state = generation;
                generation = handle.Generation;
                if (state == GENERATION_PAUSED)
                    handle.LoadMementoAndPlay(memento);
                else
                {
                    memento = memento.FromZero();
                    handle.LoadMementoAndPlay(memento);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAudioHasEnded() => throw new InvalidOperationException("Audio has already ended.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCanNotPause() => throw new InvalidOperationException("Can't pause an audio which is stopped or has finallized.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowVolumeArgumentException() => throw new ArgumentException("value", "Must be a value from 0 to 1.");
    }
}