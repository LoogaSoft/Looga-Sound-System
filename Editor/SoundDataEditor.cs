using System.Linq;
using LoogaSoft.SoundSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.SoundSystem.Editor
{
    [CustomEditor(typeof(SoundData))]
    public class SoundDataEditor : UnityEditor.Editor
    {
        public bool showPreviewButtons = true;
        
        private SerializedProperty _clipsProperty;
        private SerializedProperty _volumeProperty;
        private SerializedProperty _pitchProperty;
        private SerializedProperty _delayProperty;
        private SerializedProperty _playbackSpeedProperty;
        private SerializedProperty _playTypeProperty;
        private SerializedProperty _cyclesBeforeRepeatProperty;
        private SerializedProperty _loopTypeProperty;
        private SerializedProperty _loopCyclesProperty;

        private Rect _cachedDropArea;

        private void OnEnable()
        {
            _clipsProperty = serializedObject.FindProperty("soundClips");
            _volumeProperty = serializedObject.FindProperty("volume");
            _pitchProperty = serializedObject.FindProperty("pitch");
            _delayProperty = serializedObject.FindProperty("delay");
            _playbackSpeedProperty = serializedObject.FindProperty("playbackSpeed");
            _playTypeProperty = serializedObject.FindProperty("playType");
            _cyclesBeforeRepeatProperty = serializedObject.FindProperty("cyclesBeforeCanRepeat");
            _loopTypeProperty = serializedObject.FindProperty("loopType");
            _loopCyclesProperty = serializedObject.FindProperty("loopCycles");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawClipsWithDragDrop();
            
            EditorGUILayout.PropertyField(_volumeProperty);
            EditorGUILayout.PropertyField(_pitchProperty);
            EditorGUILayout.PropertyField(_delayProperty);
            EditorGUILayout.PropertyField(_playbackSpeedProperty);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(_playTypeProperty);
            
            if (_playTypeProperty.enumValueIndex == (int)SoundPlayType.Random)
                EditorGUILayout.PropertyField(_cyclesBeforeRepeatProperty);
            
            if (_loopTypeProperty != null)
                DrawLoopSettings();

            if (showPreviewButtons)
                DrawPreviewButtons();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawClipsWithDragDrop()
        {
            EditorGUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_clipsProperty, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            HandleDragDrop(_cachedDropArea, _clipsProperty);
            
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
                _cachedDropArea = GUILayoutUtility.GetLastRect();
        }
        
        private void DrawLoopSettings()
        {
            EditorGUILayout.PropertyField(_loopTypeProperty);

            if (_loopTypeProperty.enumValueIndex == (int)SoundLoopType.Custom)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_loopCyclesProperty);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPreviewButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Play", GUILayout.Height(30f)))
            {
                if (target is SoundData soundData)
                    EditorSoundPreviewer.PreviewSound(soundData);
            }
            if (GUILayout.Button("Stop", GUILayout.Height(30f)))
                EditorSoundPreviewer.StopPreview();
                
            EditorGUILayout.EndHorizontal();
        }

        private void HandleDragDrop(Rect dropArea, SerializedProperty listProperty)
        {
            Event e = Event.current;
            
            if (!dropArea.Contains(e.mousePosition))
                return;
            
            bool hasAudioClips = DragAndDrop.objectReferences.Any(obj => obj is AudioClip);
            if (!hasAudioClips)
                return;

            switch (e.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    e.Use();
                    break;
                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is AudioClip clip)
                        {
                            listProperty.arraySize++;
                            SerializedProperty element = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                            ResetProperty(element);
                            
                            var audioClipField = GetAudioClipField(element);
                            if (audioClipField != null) 
                                audioClipField.objectReferenceValue = clip;
                        }
                    }
                    
                    e.Use();
                    break;
            }
        }

        private SerializedProperty GetAudioClipField(SerializedProperty container)
        {
            SerializedProperty iterator = container.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            while (iterator.NextVisible(true))
            {
                if (SerializedProperty.EqualContents(iterator, end)) 
                    break;
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.type.Contains("AudioClip"))
                    return iterator;
            }
            Debug.LogError($"Could not find AudioClip field in {container.name}");
            return null;
        }

        private void ResetProperty(SerializedProperty property)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = property.GetEndProperty();

            while (iterator.NextVisible(true))
            {
                if (SerializedProperty.EqualContents(iterator, end)) 
                    break;

                switch (iterator.propertyType)
                {
                    case SerializedPropertyType.Float:
                        bool isVolOrPitch = iterator.name == "volume" || iterator.name == "pitch";
                        iterator.floatValue = isVolOrPitch ? 1f : 0f;
                        break;
                    case SerializedPropertyType.Integer: iterator.intValue = 0; break;
                    case SerializedPropertyType.Boolean: iterator.boolValue = false; break;
                    case SerializedPropertyType.ObjectReference: iterator.objectReferenceValue = null; break;
                }
            }
        }
    }
}







