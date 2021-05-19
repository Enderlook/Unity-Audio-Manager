using System.Runtime.CompilerServices;

using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Enderlook.Unity.AudioManager
{
    public partial struct AudioPlay
    {
        private const bool hidePooledObjects = true;

        private static int poolSize = 100;
        private static Handle[] pool = new Handle[poolSize];
        private static int poolIndex = -1;
        private static int totalId;

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

            return GetOrCreateHandleSlowPath();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Handle GetOrCreateHandleSlowPath()
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
    }
}