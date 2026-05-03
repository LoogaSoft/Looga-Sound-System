using LoogaSoft.SoundSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.SoundSystem.Editor
{
    [CustomPropertyDrawer(typeof(SoundData))]
    public class SoundDataPropertyDrawer : PropertyDrawer
    {
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        private UnityEditor.Editor _editor;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            bool dataValid = property.objectReferenceValue != null;

            float spacing = 2f;
            float buttonsWidth = dataValid ? (LineHeight * 2f) + spacing : 0f;
            
            float indentOffset = EditorGUI.indentLevel * 15f;
            float labelWidth = EditorGUIUtility.labelWidth - indentOffset;

            if (dataValid)
            {
                Rect foldoutRect = new Rect(position.x, position.y, labelWidth, LineHeight);
                
                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
                
                Rect objectFieldRect = new Rect(
                    position.x + labelWidth + spacing, 
                    position.y, 
                    position.width - labelWidth - buttonsWidth, 
                    LineHeight
                );
                
                EditorGUI.PropertyField(objectFieldRect, property, GUIContent.none);
                
                Rect playRect = new Rect(objectFieldRect.xMax, position.y, LineHeight, LineHeight);
                Rect stopRect = new Rect(playRect.xMax, position.y, LineHeight, LineHeight);
                
                Vector2 originalIconSize = EditorGUIUtility.GetIconSize();
                EditorGUIUtility.SetIconSize(new Vector2(LineHeight / 1.5f, LineHeight / 1.5f));
                
                GUIContent playIcon = EditorGUIUtility.IconContent("PlayButton");
                GUIContent stopIcon = EditorGUIUtility.IconContent("StopButton");
                
                if (GUI.Button(playRect, new GUIContent(playIcon.image, "Preview Sound")))
                {
                    if (property.objectReferenceValue is SoundData soundData)
                        EditorSoundPreviewer.PreviewSound(soundData);
                }
                if (GUI.Button(stopRect, new GUIContent(stopIcon.image, "Stop Preview")))
                    EditorSoundPreviewer.StopPreview();
                
                EditorGUIUtility.SetIconSize(originalIconSize);
            }
            else
            {
                float newButtonWidth = LineHeight * 2f;
                
                Rect propRect = new Rect(position.x, position.y, position.width - newButtonWidth, LineHeight);
                Rect newRect = new Rect(propRect.xMax, position.y, newButtonWidth, LineHeight);
                
                EditorGUI.PropertyField(propRect, property, label);

                if (GUI.Button(newRect, "New"))
                {
                    if (CreateSoundDataAsset(property))
                        GUIUtility.ExitGUI(); //EXIT GUI TO PREVENT ENDPROPERTY FROM THROWING AN ERROR
                }

                property.isExpanded = false;
            }

            if (property.isExpanded && dataValid)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                
                UnityEditor.Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
                
                if (_editor is SoundDataEditor soundEditor)
                    soundEditor.showPreviewButtons = false;
                
                _editor?.OnInspectorGUI();
                
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return LineHeight;
        }

        private bool CreateSoundDataAsset(SerializedProperty property)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Sound Data Asset", 
                "New Sound Data", 
                "asset", 
                "Choose a location to save the new SoundData"
            );
            
            if (string.IsNullOrEmpty(path))
                return false; //CANCELLED
            
            SoundData soundData = ScriptableObject.CreateInstance<SoundData>();
            
            AssetDatabase.CreateAsset(soundData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            property.objectReferenceValue = soundData;
            property.serializedObject.ApplyModifiedProperties();
            
            EditorGUIUtility.PingObject(soundData);
            
            return true; //SUCCESS
        }
    }
}











