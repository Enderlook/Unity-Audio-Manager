using System;
using System.Linq;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Audio;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// Represent the global settings of the audio manager.
    /// </summary>
    [CreateAssetMenu(fileName = "Global Audio Controller Configuration", menuName = "Enderlook/Audio Manager/Audio Controller Unit")]
    internal sealed class AudioControllerUnit : ScriptableObject
    {
        [SerializeField, Tooltip("Audio groups.\nThe first group is special and is used to encompass all other groups.\nDo not reorder elements of this array or audio files will get corrupted.")]
        private AudioGroup[] groups = new AudioGroup[]
        {
            new AudioGroup() { name = "Master", volume = 1 },
            new AudioGroup() { name = "Sound", volume = 1 },
            new AudioGroup() { name = "Music", volume = 1 },
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateValues()
        {
            foreach (AudioGroup group in groups)
                group.UpdateVolume();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (groups.Length == 0)
                groups = new AudioGroup[] { new AudioGroup() { name = "Master", volume = 1 } };
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref AudioGroup GetAudioGroup(string audioGroupName)
        {
            // TODO: Reduce time complexity of this from O(n) to O(1).

            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].name == audioGroupName)
                    return ref groups[i];
            }
            AudioGroupNotFound(audioGroupName);
            return ref groups[0]; // TODO: In .NET 5 use Unsafe.NullRef<T>().
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref AudioGroup GetAudioGroup(int index) => ref groups[index];

#if UNITY_EDITOR
        public string[] GetGroupNamesEditorOnly() => groups.Select(e => e.name).ToArray();
#endif

        private static void AudioGroupNotFound(string name)
            => throw new ArgumentException("audioGroupName", $"Audio group with the specified name '{name}' was not found.");

        [Serializable]
        public struct AudioGroup
        {
            [SerializeField, Tooltip("Name of the audio group.")]
            public string name;

            [SerializeField, Tooltip("Determines if this group is muted.")]
            public bool isMuted;

            [SerializeField, Range(0, 1), Tooltip("Global modifier of all audios in this group.")]
            public float volume;

            [SerializeField, Tooltip("Audio mixer group.")]
            public AudioMixerGroup audioMixerGroup;

            [SerializeField, Tooltip("Name of the variable in the Audio Mixer that controls its volume.")]
            private string volumeName;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UpdateVolume()
            {
                if (audioMixerGroup == null)
                    return;

                AudioMixer audioMixer = audioMixerGroup.audioMixer;
                if (audioMixer == null)
                    return;

                audioMixer.SetFloat(volumeName, isMuted ? -80 : (volume == 0 ? -80 : Mathf.Log(volume) * 20));
            }
        }
    }
}