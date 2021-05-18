using System.Runtime.CompilerServices;

using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    public partial struct AudioPlay
    {
        internal readonly struct Memento
        {
            public readonly IAudioFileNextEnumerator enumerator;
            public readonly AudioClip clip; // This field is required because random enumerators such as AudioBag don't store it.
            public readonly Transform follow;
            public readonly Vector3 position;
            public readonly float manualVolume;
            public readonly float time;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Memento(IAudioFileNextEnumerator enumerator, AudioClip clip, Transform follow, Vector3 position, float manualVolume, float time)
            {
                this.enumerator = enumerator;
                this.clip = clip;
                this.follow = follow;
                this.position = position;
                this.manualVolume = manualVolume;
                this.time = time;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Memento FromZero() => new Memento(enumerator, clip, follow, position, manualVolume, 0);
        }
    }
}