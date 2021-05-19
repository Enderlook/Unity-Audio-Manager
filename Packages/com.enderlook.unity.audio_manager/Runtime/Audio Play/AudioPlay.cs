using System;
using System.Runtime.CompilerServices;

using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// A handle that represent the playing of an <see cref="AudioFile"/>.
    /// </summary>
    public partial struct AudioPlay
    {
        private const bool hidePooledObjects = true;
        private static int poolSize = 100;
        private static Handle[] pool = new Handle[poolSize];
        private static int poolIndex = -1;
        private static int totalId;

        private readonly AudioFile audioFile;
        private Handle handle;
        private int generation; // If negative this contains other information described by constants named GENERATION_.
        private Memento memento;

        private const int GENERATION_PAUSED = -1;
        private const int GENERATION_STOPPED = -2;

        internal static int PoolSize {
            private get => poolSize;
            set {
                if (value == poolSize)
                    return;

                SlowPath();

                //[MethodImpl(MethodImplOptions.NoInlining)] // TODO: Add this in C# 9
                void SlowPath()
                {
                    if (poolSize < 0)
                        ThrowNegativeValue();

                    poolSize = value;
                    Handle[] newPool = new Handle[value];
                    if (value > poolIndex)
                    {
                        if (poolIndex != -1)
                            Array.Copy(pool, newPool, poolIndex);
                    }
                    else
                    {
                        Array.Copy(pool, 0, newPool, 0, poolSize);
                        for (int i = poolSize; i < poolIndex; i++)
                            UnityObject.Destroy(pool[i]);
                        poolIndex = poolSize;
                    }
                    pool = newPool;
                }
            }
        }

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
            this.audioFile = audioFile;
            handle = GetOrCreateHandle();
            generation = handle.Generation;
            handle.Feed(audioFile, loop);
            handle.Play();
            handle.TrackPosition(position);
            memento = handle.SaveMemento();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AudioPlay(AudioFile audioFile, Transform follow, bool loop = false)
        {
            this.audioFile = audioFile;
            handle = GetOrCreateHandle();
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
                handle = GetOrCreateHandle();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Handle GetOrCreateHandle()
        {
            if (poolIndex >= 0)
            {
                Handle handle = pool[poolIndex--];
                if (handle != null)
                {
                    GameObject gameObject = handle.gameObject;
                    gameObject.SetActive(true);
#if UNITY_EDITOR
                    if (hidePooledObjects)
                        gameObject.hideFlags = HideFlags.None;
#endif
                    return handle;
                }
            }

            return SlowPath();

            //[MethodImpl(MethodImplOptions.NoInlining)] // TODO: Add this in C# 9
            Handle SlowPath()
            {
                Handle handle;
                while (true)
                {
                    if (poolIndex >= 0)
                    {
                        handle = pool[poolIndex--];
                        if (handle != null)
                        {
                            GameObject gameObject = handle.gameObject;
                            gameObject.SetActive(true);
#if UNITY_EDITOR
                            if (hidePooledObjects)
                                gameObject.hideFlags = HideFlags.None;
#endif
                            break;
                        }
                    }
                    else
                    {
#if UNITY_EDITOR
                        GameObject gameObject = new GameObject($"Enderlook.Unity.AudioManager AudioSource ({totalId++})");
#else
                        GameObject gameObject = new GameObject();
#endif
                        handle = gameObject.AddComponent<Handle>();
                        break;
                    }
                }
                return handle;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Return(Handle handle)
        {
            int index = poolIndex + 1;
            if (index < PoolSize)
            {
                GameObject gameObject = handle.gameObject;
                gameObject.SetActive(false);
#if UNITY_EDITOR
                if (hidePooledObjects)
                    gameObject.hideFlags = HideFlags.HideInHierarchy;
#endif
                pool[index] = handle;
                poolIndex = index;
            }
            else
                UnityObject.Destroy(handle.gameObject);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNegativeValue() => throw new ArgumentOutOfRangeException("value", "Can't be negative");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAudioHasEnded() => throw new InvalidOperationException("Audio has already ended.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCanNotPause() => throw new InvalidOperationException("Can't pause an audio which is stopped or has finallized.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowVolumeArgumentException() => throw new ArgumentException("value", "Must be a value from 0 to 1.");
    }
}