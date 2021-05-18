using UnityEditor;

using UnityEngine;

namespace Enderlook.Unity.AudioManager
{
    internal static class Utility
    {
        private static readonly GUIStyle popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"))
        {
            imagePosition = ImagePosition.ImageOnly
        };

        public static int Draw(GUIContent label, Rect position, string[] options, int optionIndex, out Rect newPosition)
        {
            // Show field label
            newPosition = EditorGUI.PrefixLabel(position, label);

            // Calculate rect for configuration button
            Rect buttonRect = new Rect(
                newPosition.x,
                newPosition.y + popupStyle.margin.top,
                popupStyle.fixedWidth + popupStyle.margin.right,
                newPosition.height - popupStyle.margin.top);

            newPosition.xMin += buttonRect.width;

            return EditorGUI.Popup(buttonRect, optionIndex, options, popupStyle);
        }
    }
}