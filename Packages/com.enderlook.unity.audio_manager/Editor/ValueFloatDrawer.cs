using System.Reflection;

using UnityEditor;

using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    [CustomPropertyDrawer(typeof(ValueFloat))]
    internal sealed class ValueFloatDrawer : PropertyDrawer
    {
        private readonly string[] popupOptions = new string[] { "Constant", "Random" };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty isRandom = property.FindPropertyRelative("isRandom");
            int popupIndex = isRandom.boolValue ? 1 : 0;

            int newPopupIndex = Utility.Draw(label, position, popupOptions, popupIndex, out Rect newPosition);

            if (newPopupIndex != popupIndex)
                isRandom.boolValue = newPopupIndex == 1;

            switch (newPopupIndex)
            {
                case 0:
                {
                    SerializedProperty constant = property.FindPropertyRelative("a");
                    ValueRangeFloatAttribute range = fieldInfo.GetCustomAttribute<ValueRangeFloatAttribute>(true);
                    if (range is null)
                        constant.floatValue = EditorGUI.FloatField(newPosition, constant.floatValue);
                    else
                        constant.floatValue = EditorGUI.Slider(newPosition, constant.floatValue, range.Min, range.Max);
                    break;
                }
                case 1:
                {
                    SerializedProperty min = property.FindPropertyRelative("a");
                    SerializedProperty max = property.FindPropertyRelative("b");
                    float minValue = min.floatValue;
                    float maxValue = max.floatValue;

                    ValueRangeFloatAttribute range = fieldInfo.GetCustomAttribute<ValueRangeFloatAttribute>(true);
                    if (range is null)
                    {
                        const float spacing = 1;

                        Rect firstRect = position;
                        firstRect.width = (firstRect.width / 2) - spacing;

                        Rect secondRect = firstRect;
                        secondRect.x += firstRect.width + (spacing * 2);

                        minValue = EditorGUI.FloatField(firstRect, minValue);
                        maxValue = EditorGUI.FloatField(secondRect, maxValue);
                    }
                    else
                    {
                        const float spacing = 1;

                        Rect firstRect = newPosition;
                        firstRect.width = (firstRect.width / 4) - spacing;

                        Rect secondRect = firstRect;
                        secondRect.x += firstRect.width + (spacing * 2);
                        secondRect.width = (newPosition.width / 2) - spacing;

                        Rect thirdRect = secondRect;
                        thirdRect.x += secondRect.width + (spacing * 2);
                        thirdRect.width = firstRect.width;

                        minValue = EditorGUI.FloatField(firstRect, minValue);
                        EditorGUI.MinMaxSlider(secondRect, ref minValue, ref maxValue, range.Min, range.Max);
                        maxValue = EditorGUI.FloatField(thirdRect, maxValue);

                        minValue = Mathf.Clamp(minValue, range.Min, range.Max);
                        maxValue = Mathf.Clamp(maxValue, range.Min, range.Max);
                    }

                    if (minValue > maxValue)
                        maxValue = minValue;
                    else if (maxValue < minValue)
                        minValue = maxValue;
                    min.floatValue = minValue;
                    max.floatValue = maxValue;

                    break;
                }
            }
        }
    }
}