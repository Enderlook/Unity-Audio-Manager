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
        private readonly Transform follow;
        private readonly Vector3 position;
        private Handle handle;
        private int generation;
        private float storedDuration;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AudioPlay Play(AudioFile audioFile, Vector3 location, bool loop = false)
            => new AudioPlay(audioFile, location, loop);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AudioPlay Play(AudioFile audioFile, Transform follow, bool loop = false)
            => new AudioPlay(audioFile, follow, loop);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AudioPlay(AudioFile audioFile, bool loop = false)
        {
            handle = GetOrCreateHandle(audioFile);
            handle.Play();
            this.audioFile = audioFile;
            generation = handle.Generation;
            storedDuration = 0;
            handle.SetLoop(loop);
            position = default;
            follow = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AudioPlay(AudioFile audioFile, Vector3 position, bool loop = false) : this(audioFile, loop)
        {
            this.position = position;
            handle.TrackPosition(position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AudioPlay(AudioFile audioFile, Transform follow, bool loop = false) : this(audioFile, loop)
        {
            this.follow = follow;
            handle.TrackPosition(position);
        }

        /// <summary>
        /// Stop the execution of an audio.
        /// </summary>
        public void Stop()
        {
            if (generation == handle.Generation)
            {
                handle.Stop();
                Return(this);
            }
        }

        /// <summary>
        /// Pause the execution of an audio.
        /// </summary>
        public void Pause()
        {
            if (generation == handle.Generation)
            {
                storedDuration = handle.Pause();
                Return(this);
            }
        }

        /// <summary>
        /// Reanude the execution of an audio from <see cref="Pause"/> or start over if <see cref="Stop"/> was executed or it finalized playing.
        /// </summary>
        public void Play()
        {
            if (generation != handle.Generation)
            {
                handle = GetOrCreateHandle(audioFile);
                generation = handle.Generation;
            }

            handle.PlayFrom(storedDuration);
            storedDuration = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Handle GetOrCreateHandle(AudioFile audioFile)
        {
            if (poolIndex >= 0)
            {
                Handle handle = pool[poolIndex--];
                if (handle.gameObject != null)
                {
                    GameObject gameObject = handle.gameObject;
                    gameObject.SetActive(true);
#if UNITY_EDITOR
                    if (hidePooledObjects)
                        gameObject.hideFlags = HideFlags.None;
#endif
                    handle.Initialize(audioFile);
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
                        if (handle.gameObject != null)
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
                handle.Initialize(audioFile);
                return handle;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Return(AudioPlay handler) => Return(handler.handle);

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
        private static void ThrowVolumeArgumentException() => throw new ArgumentException("value", "Must be a value from 0 to 1.");
    }
}