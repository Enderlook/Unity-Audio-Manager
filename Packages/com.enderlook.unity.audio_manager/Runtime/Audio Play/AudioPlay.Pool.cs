using System;
using System.Runtime.CompilerServices;

using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Enderlook.Unity.AudioManager
{
    public partial struct AudioPlay
    {
        private static class Pool
        {
            private const int INITIAL_CAPACITY = 16;
            // Just a random large enough power of GROW_FACTOR;
            private const int MAXIMUM_CAPACITY = 32768;
            private const float GROW_FACTOR = 2;
            private const float SHRINK_FACTOR = 2;
            private const float SHRINK_THRESHOLD = 1/3;
            // We don't remove all objects, but a percentage of current objects in the pool, to prevent reallocating much.
            private const float REMOVAL_FACTOR = .35f;

            private static Handle[] pool = new Handle[INITIAL_CAPACITY];
            private static int poolIndex = -1;

            private static int totalId;

            // Prevent the execution of multiple clears too frequently.
            // Useful when GC is incremental or non generational and so the finalizer object may be collected too frequently.
            private const float MINIMUM_TIME_BETWEEN_CLEARS_IN_SECONDS = 30;
            private static bool isClearingPoolRequested;
            private static float allowClearAt;

            static Pool() => new Gen2CollectCallback();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Handle GetOrCreateHandle()
            {
                if (poolIndex >= 0)
                {
                    Handle handle = pool[poolIndex--];
                    if (handle != null)
                    {
                        GameObject gameObject = handle.gameObject;
                        gameObject.SetActive(true);
#if UNITY_EDITOR
                        if (AudioController.HidePooledObjects)
                            gameObject.hideFlags = HideFlags.None;
#endif
                        return handle;
                    }
                }

                return GetOrCreateHandleSlowPath();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static Handle GetOrCreateHandleSlowPath()
            {
                Handle[] pool_ = pool;

                pool_[poolIndex + 1] = null; // Clear the reference which may contains a Unity null.

                Handle handle;
                while (true)
                {
                    if (poolIndex >= 0)
                    {
                        ref Handle slot = ref pool_[poolIndex--];
                        if (slot != null)
                        {
                            handle = slot;
                            GameObject gameObject = handle.gameObject;
                            gameObject.SetActive(true);
#if UNITY_EDITOR
                            if (AudioController.HidePooledObjects)
                                gameObject.hideFlags = HideFlags.None;
#endif
                            break;
                        }
                        else
                            slot = null; // Clear the reference which may contains a Unity null.
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Return(Handle handle)
            {
                Handle[] pool_ = pool;
                int index = poolIndex + 1;
                if (index < pool_.Length)
                {
                    GameObject gameObject = handle.gameObject;
                    gameObject.SetActive(false);
#if UNITY_EDITOR
                    if (AudioController.HidePooledObjects)
                        gameObject.hideFlags = HideFlags.HideInHierarchy;
#endif
                    pool_[index] = handle;
                    poolIndex = index;
                }
                else
                    ReturnSlowPath(handle);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ReturnSlowPath(Handle handle)
            {
                Handle[] pool_ = pool;
                int poolLength = pool_.Length;

                int newLength = (int)Mathf.Min(poolLength * GROW_FACTOR, MAXIMUM_CAPACITY);
                if (poolLength == MAXIMUM_CAPACITY)
                {
                    UnityObject.Destroy(handle);
                    return;
                }

                Array.Resize(ref pool_, newLength);
                pool = pool_;

                int index = poolIndex + 1;
                GameObject gameObject = handle.gameObject;
                gameObject.SetActive(false);
#if UNITY_EDITOR
                if (AudioController.HidePooledObjects)
                    gameObject.hideFlags = HideFlags.HideInHierarchy;
#endif
                pool_[index] = handle;
                poolIndex = index;
            }

            private sealed class Gen2CollectCallback
            {
                ~Gen2CollectCallback()
                {
                    // We can't execute clearing here because we are in the finalizer thread and clearing must happen in the Unity thread.
                    isClearingPoolRequested = true;
                    GC.ReRegisterForFinalize(this);
                }
            }

            internal static void TryClearPool()
            {
                if (!isClearingPoolRequested || Time.realtimeSinceStartup < allowClearAt)
                    return;

                isClearingPoolRequested = false;
                allowClearAt = Time.realtimeSinceStartup + MINIMUM_TIME_BETWEEN_CLEARS_IN_SECONDS;

                int poolIndex_ = poolIndex;
                if (poolIndex == -1)
                    return;

                Handle[] pool_ = pool;

                Handle _ = pool_[poolIndex_];

                int toRemoveCount = Mathf.CeilToInt(poolIndex_ * REMOVAL_FACTOR);
                for (int i = 0; i < toRemoveCount; i++)
                {
                    Handle handle = pool_[poolIndex_--];
                    if (handle != null)
                        UnityObject.Destroy(handle.gameObject);
                }

                int poolLength = pool_.Length;
                if (poolLength > INITIAL_CAPACITY)
                {
                    Debug.Assert(SHRINK_THRESHOLD <= SHRINK_FACTOR);

                    int count = poolLength;
                    while (true)
                    {
                        if ((float)poolIndex_ / count < SHRINK_THRESHOLD)
                        {
                            count = (int)(count / SHRINK_FACTOR);
                            if (count <= INITIAL_CAPACITY)
                            {
                                count = INITIAL_CAPACITY;
                                break;
                            }
                        }
                        else
                            break;
                    }

                    if (count != poolLength)
                    {
                        Handle[] newPool = new Handle[count];
                        Array.Copy(pool_, newPool, poolIndex_);
                        pool = newPool;
                    }
                }

                poolIndex = poolIndex_;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TryClearPool() => Pool.TryClearPool();
    }
}
