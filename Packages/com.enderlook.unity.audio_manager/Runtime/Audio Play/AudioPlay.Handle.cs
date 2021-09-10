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
            public int Generation { get; private set; } = 1;
            private float returnAt;

            private Transform follow;
            private IAudioFileNextEnumerator enumerator;

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
            public void Feed(AudioFile audioFile, bool loop)
            {
                Debug.Assert(audioFile != null);
                IAudioFileNextEnumerator enumerator_ = audioFile.StartEnumerator(audioSource, loop);
                Debug.Assert(enumerator_ != null);
                enumerator = enumerator_;
                automaticVolume = audioSource.volume;
                manualVolume = 1;
                returnAt = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Memento SaveMemento() => new Memento(enumerator, audioSource.clip, follow, transform.position, Volume, audioSource.time);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Memento Pause()
            {
                Memento memento = new Memento(enumerator, audioSource.clip, follow, transform.position, Volume, audioSource.time);
                audioSource.Stop();
                Return();
                return memento;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Memento Stop()
            {
                Memento memento = new Memento(enumerator, audioSource.clip, follow, transform.position, Volume, 0);
                audioSource.Stop();
                Return();
                return memento;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void LoadMementoAndPlay(in Memento memento)
            {
                transform.position = memento.position;
                follow = memento.follow;
                audioSource.clip = memento.clip;
                IAudioFileNextEnumerator enumerator_ = memento.enumerator;
                enumerator = enumerator_;
                enumerator_.ApplyCurrent(audioSource);
                Volume = memento.manualVolume;
                audioSource.Play();
                audioSource.time = memento.time;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetPosition(Vector3 position) => transform.position = position;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void FollowTransform(Transform follow)
            {
                SetPosition(follow.position);
                this.follow = follow;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Play() => audioSource.Play();

            private void Update()
            {
                if (!audioSource.isPlaying)
                {
                    if (enumerator.MoveNext(audioSource))
                        audioSource.Play();
                    else
                        Return();
                }
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
                            {
                                returnAt = 0;
                                if (enumerator.MoveNext(audioSource))
                                    audioSource.Play();
                                else
                                    Return();
                            }
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
                enumerator = null;
                Pool.Return(this);
            }
        }
    }
}