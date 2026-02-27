using LoogaSoft.SoundSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.SoundSystem.Editor
{
    [CustomPropertyDrawer(typeof(SoundClip))]
    public class SoundClipDrawer : PropertyDrawer
    {
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        private static readonly float Spacing = EditorGUIUtility.standardVerticalSpacing;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            SerializedProperty initializedProp = property.FindPropertyRelative("_initialized");
            if (!initializedProp.boolValue)
            {
                property.FindPropertyRelative("volume").floatValue = 1f;
                property.FindPropertyRelative("pitch").floatValue = 1f;
                initializedProp.boolValue = true;
            }
            
            float clipWidth = position.width - (LineHeight * 2f);
            
            Rect clipRect = new Rect(position.x, position.y, clipWidth, LineHeight);
            Rect button1Rect = new Rect(clipRect.xMax, position.y, LineHeight, LineHeight);
            Rect button2Rect = new Rect(button1Rect.xMax, position.y, LineHeight, LineHeight);
            
            EditorGUI.PropertyField(clipRect, property.FindPropertyRelative("clip"));

            Vector2 originalIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(LineHeight / 1.5f, LineHeight / 1.5f));;
            
            GUIContent playIcon = EditorGUIUtility.IconContent("PlayButton");
            playIcon.tooltip = "Play Clip";

            if (GUI.Button(button1Rect, playIcon))
            {
                if (property.boxedValue is SoundClip soundClip)
                    EditorSoundPreviewer.PreviewSound(soundClip);
            }
            
            GUIContent stopIcon = EditorGUIUtility.IconContent("StopButton");
            stopIcon.tooltip = "Stop Clip";

            if (GUI.Button(button2Rect, stopIcon))
            {
                EditorSoundPreviewer.StopPreview();
            }
            
            EditorGUIUtility.SetIconSize(originalIconSize);

            float spacing = 5f;
            float fieldWidth = (position.width - (spacing * 2f)) / 3f;
            float secondLineY = position.y + LineHeight + Spacing;
            
            SerializedProperty volumeProp = property.FindPropertyRelative("volume");
            SerializedProperty pitchProp = property.FindPropertyRelative("pitch");
            SerializedProperty delayProp = property.FindPropertyRelative("delay");

            Rect volumeRect = new Rect(position.x, secondLineY, fieldWidth, LineHeight * 2f);
            Rect pitchRect = new Rect(position.x + fieldWidth + spacing, secondLineY, fieldWidth, LineHeight * 2f);
            Rect delayRect = new Rect(position.x + (fieldWidth + Spacing) * 2f + spacing, secondLineY, fieldWidth, LineHeight * 2f);
            
            DrawFieldWithLabel(volumeRect, volumeProp, "Volume");
            DrawFieldWithLabel(pitchRect, pitchProp, "Pitch");
            DrawFieldWithLabel(delayRect, delayProp, "Delay");
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (LineHeight * 3f) + (Spacing * 2f);
        }
        private void DrawFieldWithLabel(Rect rect, SerializedProperty property, string label)
        {
            Rect labelRect = new Rect(rect.x, rect.y, rect.width, LineHeight);
            Rect fieldRect = new Rect(rect.x, rect.y + LineHeight, rect.width, LineHeight);
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            EditorGUI.LabelField(labelRect, label, labelStyle);
            
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
        }
    }
}