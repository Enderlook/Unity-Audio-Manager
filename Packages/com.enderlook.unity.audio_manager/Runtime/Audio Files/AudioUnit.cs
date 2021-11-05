using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// Represent a single <see cref="AudioClip"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Audio Unit", menuName = "Enderlook/Audio Manager/Audio Unit")]
    public sealed class AudioUnit : AudioFile
    {
        [SerializeField, Tooltip("Audio clip of this unit.")]
        private AudioClip audioClip;

        [SerializeField, Tooltip("Audio group that this audio belongs to.")]
        private string audioGroup;

        [SerializeField, ValueRangeFloat(0, 1), Tooltip("Relative volume of this audio.")]
        private ValueFloat volume = new ValueFloat(1);

        [SerializeField, ValueRangeFloat(-3, 3), Tooltip("Relative pitch of this audio.")]
        private ValueFloat pitch = new ValueFloat(1);

        [SerializeField, Range(0, 256), Tooltip("Sets the priority of the source. " +
            "Note that a sound with a larger priority value will more likely be stolen by sounds with smaller priority values.")]
        private byte priority = 128;

        [SerializeField, Range(-1, 1), Tooltip("Only valid for Mono and Stereo AudioClips. " +
            "Mono sounds will be panned at constant power left and right. " +
            "Stereo sounds will have each left/right value faded up and down according to the specified pan value.")]
        private float stereoSpan;

        [SerializeField, Tooltip("Sets how much the AudioSource that will play this clip is treated as a 3D source. " +
            "3D sources are affected by spatial position and spread." +
            "If a 3D Pan Level is 0, all spatial attenaution is ignored.")]
        private AnimationCurve spatialBlend = new AnimationCurve(new Keyframe(0, 1));

        [SerializeField, Tooltip("Sets how much of the signal the AudioSource that will play this clip is mixing into the global reveb associated with the zones. " +
            "[0, 1] is a lineal range (like volume) while [1, 1.1] lest you boost the reverb mix by 10 dB.")]
        private AnimationCurve reverbZoneMix = new AnimationCurve(new Keyframe(0, 1));

        [SerializeField, Range(0, 5f), Tooltip("Specifies how much the pitch is changed based on the relative velocity between AudioListener and the AudioSource tht will play this clip.")]
        private float dopplerLevel = 1;

        [SerializeField, Tooltip("Sets the spread of a 3d sound in speaker space.")]
        private AnimationCurve spread = new AnimationCurve(new Keyframe(0, 0));

        [SerializeField, Tooltip("Which type of rolloff curve to use.")]
        private AudioRolloffMode volumeRolloff;

        [SerializeField, Tooltip("Withing the minDistance, the volume will stay at the loudest possible. " +
            "Outside of this minDistance it beings to attenuate.")]
        private float minDistance = 1;

        [SerializeField, Tooltip("MaxDistance is the distance a sound stops attenuating at.")]
        private float maxDistance = 500;

        [SerializeField]
        private AnimationCurve customRolloffCurve;

        private OnceEnumerator once;
        private LoopEnumerator loop;

        internal override IAudioFileNextEnumerator StartEnumerator(AudioSource audioSource, bool loop)
        {
            Set(audioSource);
            if (loop)
                return this.loop ?? (this.loop = new LoopEnumerator(this));
            else
                return once ?? (once = new OnceEnumerator(this));
        }

        private void Set(AudioSource audioSource)
        {
#if DEBUG
            if (audioClip == null)
                Debug.LogError("Audio Clip property of Audio Unit was null. This produced undefined behaviour. This error is only reported on debug.");
#endif

            AudioController.SetBasicToAudioSource(audioSource, audioGroup);
            audioSource.clip = audioClip;
            audioSource.pitch = pitch.Value;
            audioSource.volume = volume.Value;
            audioSource.priority = priority;
            audioSource.panStereo = stereoSpan;

            if (spatialBlend.length == 1)
                audioSource.spatialBlend = spatialBlend[0].value;
            else
                audioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, spatialBlend);

            if (reverbZoneMix.length == 1)
                audioSource.reverbZoneMix = reverbZoneMix[0].value;
            else
                audioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, reverbZoneMix);

            if (spread.length == 1)
                audioSource.spread = spread[0].value;
            else
                audioSource.SetCustomCurve(AudioSourceCurveType.Spread, spread);

            audioSource.dopplerLevel = dopplerLevel;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = volumeRolloff;

            if (volumeRolloff == AudioRolloffMode.Custom)
                audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloffCurve);
        }

        private sealed class OnceEnumerator : IAudioFileNextEnumerator
        {
            private readonly AudioUnit unit;

            public OnceEnumerator(AudioUnit unit)
            {
                Debug.Assert(!(unit is null));
                this.unit = unit;
            }

            public void ApplyCurrent(AudioSource audioSource) => unit.Set(audioSource);

            public bool MoveNext(AudioSource source) => false;
        }

        private sealed class LoopEnumerator : IAudioFileNextEnumerator
        {
            private readonly AudioUnit unit;

            public LoopEnumerator(AudioUnit unit)
            {
                Debug.Assert(!(unit is null));
                this.unit = unit;
            }

            public void ApplyCurrent(AudioSource audioSource) => unit.Set(audioSource);

            public bool MoveNext(AudioSource audioSource)
            {
                unit.Set(audioSource);
                return true;
            }
        }
    }
}