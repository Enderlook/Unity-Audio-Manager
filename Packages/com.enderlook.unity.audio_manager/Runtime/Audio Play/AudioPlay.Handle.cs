using System.Runtime.CompilerServices;

using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    public partial struct AudioPlay
    {
        [AddComponentMenu(""), RequireComponent(typeof(AudioSource))] // Hide from menu.
        internal sealed class Handle : MonoBehaviour
        {
            private AudioSource audioSource;
            public int Generation { get; private set; }
            private Transform follow;

            private float returnAt;

            private float automaticVolume;
            private float manualVolume;
            public float Volume {
                get => manualVolume;
                set {
                    manualVolume = value;
                    audioSource.volume = automaticVolume * manualVolume;
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
            private void Awake()
            {
                audioSource = GetComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Initialize(AudioFile audioFile)
            {
                Debug.Assert(audioFile != null);
                audioFile.ConfigureAudioSource(audioSource);
                automaticVolume = audioSource.volume;
                manualVolume = 1;
                returnAt = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetLoop(bool loop) => audioSource.loop = loop;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void TrackPosition(Vector3 position) => transform.position = position;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void TrackPosition(Transform follow)
            {
                TrackPosition(follow.position);
                this.follow = follow;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Play() => audioSource.Play();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PlayFrom(float time)
            {
                audioSource.Play();
                audioSource.time = time;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Stop() => audioSource.Stop();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Pause()
            {
                audioSource.Pause();
                return audioSource.time;
            }

            private void Update()
            {
                if (!audioSource.isPlaying)
                    Return();
                else
                {
                    // Prevent bug when audioSource get stuck with isPlaying is true but actually it's not playing.
                    if (audioSource.time == 0)
                    {
                        if (returnAt < Time.time)
                        {
                            if (returnAt == 0)
                            {
                                returnAt = Time.time + 1;
                                audioSource.Play();
                            }
                            else
                                Return();
                        }
                    }
                    else
                        returnAt = 0;

                    if (follow != null)
                        transform.position = follow.position;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Return()
            {
                Generation++;
                // Set to null in order to allow the GC collect it.
                follow = null;
                AudioPlay.Return(this);
            }
        }
    }
}