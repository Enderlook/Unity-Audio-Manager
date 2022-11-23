using Enderlook.Unity.Toolset.Utils;

using System;

using UnityEditor;

using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Enderlook.Unity.AudioManager
{
    internal static class ContextMenu
    {
        [MenuItem("Assets/Enderlook/Audio Manager/Create Audio Unit")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private static void CreateAudioUnit()
        {
            foreach (UnityObject selection in Selection.objects)
            {
                if (selection is AudioClip audioClip)
                {
                    AudioUnit audioUnit = AudioUnit.Create(audioClip);
                    string path = $"{AssetDatabaseHelper.GetAssetDirectory(selection)}/{AssetDatabaseHelper.GetAssetFileNameWithoutExtension(selection)}.asset";
                    AssetDatabaseHelper.CreateAsset(audioUnit, path);
                }
            }
        }

        [MenuItem("Assets/Enderlook/Audio Manager/Create Audio Unit", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private static bool CreateAudioUnitValidate()
        {
            foreach (UnityObject selection in Selection.objects)
            {
                if (selection is AudioClip)
                    return true;
            }
            return false;
        }

        [MenuItem("Assets/Enderlook/Audio Manager/Create Audio Bag")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private static void CreateAudioBag()
        {
            (string path, AudioFile[] files, (AudioFile file, string[] path, bool mustAppend)[] tmp) tuple = GetFilesAndPath();
            if (tuple.path is null)
                return;
            AudioBag bag = AudioBag.Create(tuple.files);
            CreateAsset(bag, tuple.path + "/Audio Bag.asset", tuple.tmp);
        }

        [MenuItem("Assets/Enderlook/Audio Manager/Create Audio Sequence")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private static void CreateAudioSequence()
        {
            (string path, AudioFile[] files, (AudioFile file, string[] path, bool mustAppend)[] tmp) tuple = GetFilesAndPath();
            if (tuple.path is null)
                return;
            AudioSequence bag = AudioSequence.Create(tuple.files);
            CreateAsset(bag, tuple.path + "/Audio Sequence.asset", tuple.tmp);
        }

        [MenuItem("Assets/Enderlook/Audio Manager/Create Audio Bag", true)]
        [MenuItem("Assets/Enderlook/Audio Manager/Create Audio Sequence", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private static bool CreateAudioCollectionValidate()
        {
            foreach (UnityObject selection in Selection.objects)
            {
                if (selection is AudioClip || selection is AudioFile)
                    return true;
            }
            return false;
        }

        private static (string path, AudioFile[] files, (AudioFile file, string[] path, bool mustAppend)[] tmp) GetFilesAndPath()
        {
            (AudioFile file, string[] path, bool mustAppend)[] files = new (AudioFile file, string[] path, bool mustAppend)[Selection.objects.Length];
            int i = 0;
            int maxLength = 0;
            foreach (UnityObject selection in Selection.objects)
            {
                if (selection is AudioFile audioFile)
                {
                    string[] path = AssetDatabaseHelper.GetAssetDirectory(selection).Split('/');
                    maxLength = Math.Max(maxLength, path.Length);
                    files[i++] = (audioFile, path, false);
                }
                else if (selection is AudioClip audioClip)
                {
                    string[] path = AssetDatabaseHelper.GetAssetDirectory(selection).Split('/');
                    maxLength = Math.Max(maxLength, path.Length);
                    AudioFile file = AudioUnit.Create(audioClip);
                    file.name = AssetDatabaseHelper.GetAssetFileNameWithoutExtension(selection);
                    files[i++] = (file, path, true);
                }
            }

            if (i == 0)
                return default;

            Span<(AudioFile file, string[] path, bool mustAppend)> span = files.AsSpan(0, i);
            string[] commonPath = new string[maxLength];
            for (i = 0; i < commonPath.Length; i++)
            {
                string[] path_ = span[0].path;
                if (path_.Length <= i)
                    break;
                string common = path_[i];
                for (int j = 1; j < span.Length; j++)
                {
                    path_ = span[j].path;
                    if (path_.Length <= i || common != path_[i])
                        goto end;
                }
                commonPath[i] = common;
            }
        end:

            string newPath = string.Join("/", commonPath, 0, i);
            AudioFile[] resultFiles = new AudioFile[span.Length];
            for (i = 0; i < resultFiles.Length; i++)
                resultFiles[i] = span[i].file;

            return (newPath, resultFiles, files);
        }

        private static void CreateAsset(UnityObject asset, string path, Span<(AudioFile file, string[] path, bool mustAppend)> span)
        {
            AssetDatabaseHelper.CreateAsset(asset, path, true);
            for (int i = 0; i < span.Length; i++)
            {
                (AudioFile file, string[] path, bool mustAppend) element = span[i];
                if (element.mustAppend)
                    AssetDatabase.AddObjectToAsset(element.file, asset);
            }
            AssetDatabase.SaveAssets();
        }
    }
}