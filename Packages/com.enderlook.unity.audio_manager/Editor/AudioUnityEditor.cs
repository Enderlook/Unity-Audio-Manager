using Enderlook.Unity.Toolset.Utils;

using UnityEditor;

using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    [CustomEditor(typeof(AudioUnit)), CanEditMultipleObjects]
    internal sealed class AudioUnityEditor : Editor
    {
        private static readonly GUIContent D3_SOUND_SETTINGS_CONTENT = new GUIContent("3D Sound Settings", "3D configuration of the audio file.");
        private static readonly GUIContent REVERB_ZONE_MIX_CONTENT = new GUIContent("");
        private static readonly GUIContent SPATIAL_BLEND_CONTENT = new GUIContent("");
        private static readonly GUIContent SPREAD_CONTENT = new GUIContent("");

        private static readonly string[] options = new string[] { "Constant", "Curve" };
        private SerializedProperty audioClip;
        private SerializedProperty audioType;
        private SerializedProperty volume;
        private SerializedProperty pitch;
        private SerializedProperty priority;
        private SerializedProperty stereoSpan;
        private SerializedProperty spatialBlend;
        private SerializedProperty reverbZoneMix;
        private SerializedProperty dopplerLevel;
        private SerializedProperty spread;
        private SerializedProperty volumeRolloff;
        private SerializedProperty minDistance;
        private SerializedProperty maxDistance;
        private SerializedProperty customRolloffCurve;

        private bool is3DSoundsSettingsExpanded = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void OnEnable()
        {
            audioClip = serializedObject.FindProperty("audioClip");
            audioType = serializedObject.FindProperty("audioType");
            volume = serializedObject.FindProperty("volume");
            pitch = serializedObject.FindProperty("pitch");
            priority = serializedObject.FindProperty("priority");
            stereoSpan = serializedObject.FindProperty("stereoSpan");
            spatialBlend = serializedObject.FindProperty("spatialBlend");
            reverbZoneMix = serializedObject.FindProperty("reverbZoneMix");
            dopplerLevel = serializedObject.FindProperty("dopplerLevel");
            spread = serializedObject.FindProperty("spread");
            volumeRolloff = serializedObject.FindProperty("volumeRolloff");
            minDistance = serializedObject.FindProperty("minDistance");
            maxDistance = serializedObject.FindProperty("maxDistance");
            customRolloffCurve = serializedObject.FindProperty("customRolloffCurve");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        //private void OnDisable() => DestroyImmediate(audioSource.gameObject);

        public override void OnInspectorGUI()
        {
            this.DrawScriptField();

            EditorGUILayout.PropertyField(audioClip);
            EditorGUILayout.PropertyField(audioType);
            EditorGUILayout.PropertyField(volume);
            EditorGUILayout.PropertyField(pitch);
            EditorGUILayout.PropertyField(priority);
            EditorGUILayout.PropertyField(stereoSpan);

            DrawFieldOrCurve(spatialBlend, SPATIAL_BLEND_CONTENT, 0, 1f, 1);
            DrawFieldOrCurve(reverbZoneMix, REVERB_ZONE_MIX_CONTENT, 0, 1.1f, 1);

            EditorGUILayout.Space();

            if (is3DSoundsSettingsExpanded = EditorGUILayout.Foldout(is3DSoundsSettingsExpanded, D3_SOUND_SETTINGS_CONTENT, true))
            {
                EditorGUILayout.PropertyField(dopplerLevel);
                DrawFieldOrCurve(spread, SPREAD_CONTENT, 0, 360, 0);
                EditorGUILayout.PropertyField(volumeRolloff);

                if ((AudioRolloffMode)volumeRolloff.enumValueIndex == AudioRolloffMode.Custom)
                {
                    AnimationCurve curve = customRolloffCurve.animationCurveValue;
                    if (curve is null)
                        curve = new AnimationCurve();
                    curve.AddKey(0, 1);
                    curve.AddKey(1, 1);

                    EditorGUILayout.CurveField(curve);
                    ClampCurve(0, 1.1f, customRolloffCurve.animationCurveValue);
                    customRolloffCurve.animationCurveValue = curve;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(minDistance);
                if (EditorGUI.EndChangeCheck())
                    minDistance.floatValue = Mathf.Min(Mathf.Max(minDistance.floatValue, 0.01f), 1000000.0f);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(maxDistance);
                if (EditorGUI.EndChangeCheck())
                    maxDistance.floatValue = Mathf.Min(Mathf.Max(Mathf.Max(maxDistance.floatValue, 0.01f), minDistance.floatValue * 1.01f), 1000000.0f);
            }
            else
            {
                // Fix curve if broken.
                AnimationCurve curve = spread.animationCurveValue;
                if (curve.length == 0)
                    curve.AddKey(0, 0);
                spread.animationCurveValue = curve;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawFieldOrCurve(SerializedProperty property, GUIContent content, float min, float max, float @default)
        {
            if (string.IsNullOrEmpty(content.text))
            {
                content.text = property.displayName;
                content.tooltip = property.tooltip;
            }

            AnimationCurve curve = property.animationCurveValue;
            if (curve is null)
                curve = new AnimationCurve();
            if (curve.length == 0)
                curve.AddKey(0, @default);

            int mode = curve.length == 1 ? 0 : 1;

            Rect rect = EditorGUILayout.GetControlRect(true);

            switch (Utility.Draw(content, rect, options, mode, out Rect newRect))
            {
                case 0:
                    Keyframe keyframe = curve[0];
                    if (mode == 1)
                    {
                        float value_ = keyframe.value;
                        while (curve.length > 0)
                            curve.RemoveKey(curve.length - 1);
                        curve.AddKey(0, value_);
                    }
                    float value = EditorGUI.Slider(newRect, keyframe.value, min, max);
                    if (value != keyframe.value)
                    {
                        curve.RemoveKey(0);
                        curve.AddKey(0, value);
                    }
                    break;
                case 1:
                    if (mode == 0)
                        curve.AddKey(1, curve[0].value);
                    EditorGUI.BeginChangeCheck();
                    curve = EditorGUI.CurveField(newRect, curve);
                    if (EditorGUI.EndChangeCheck())
                        ClampCurve(min, max, curve);
                    break;
            }

            property.animationCurveValue = curve;
        }

        private static void ClampCurve(float min, float max, AnimationCurve curve)
        {
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                ref Keyframe key = ref keys[i];
                key.value = Mathf.Clamp(key.value, min, max);
            }
        }
    }
}