﻿using System;
using System.Runtime.CompilerServices;

using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Enderlook.Unity.AudioManager
{
    public partial struct AudioPlay
    {
        private static int poolSize = 100;
        private static Handle[] pool = new Handle[poolSize];
        private static int poolIndex = -1;

        private static int totalId;

        // Prevent the execution of multiple clears too frequently.
        // Useful when GC is incremental or non generational and so the finalizer object may be collected too frequently.
        private const float minimumTimeBetweenClears = 60;
        private static bool isClearingPoolRequested;
        private static float allowClearAt;

        static AudioPlay() => new Gen2CollectCallback();

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
        private static void Return(Handle handle)
        {
            int index = poolIndex + 1;
            if (index < PoolSize)
            {
                GameObject gameObject = handle.gameObject;
                gameObject.SetActive(false);
#if UNITY_EDITOR
                if (AudioController.HidePooledObjects)
                    gameObject.hideFlags = HideFlags.HideInHierarchy;
#endif
                pool[index] = handle;
                poolIndex = index;
            }
            else
                UnityObject.Destroy(handle.gameObject);
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
            if (!isClearingPoolRequested || allowClearAt <= Time.realtimeSinceStartup)
                return;

            isClearingPoolRequested = false;
            allowClearAt = Time.realtimeSinceStartup + minimumTimeBetweenClears;

            if (poolIndex == -1)
                return;

            Handle[] pool_ = pool;
            for (int i = 0; i <= poolIndex; i++)
            {
                Handle handle = pool_[i];
                if (handle != null)
                    UnityObject.Destroy(handle.gameObject);
            }

            Array.Clear(pool_, 0, pool_.Length);
            poolIndex = 0;
        }
    }
}