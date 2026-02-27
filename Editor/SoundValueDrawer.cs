using LoogaSoft.SoundSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.SoundSystem.Editor
{
    [CustomPropertyDrawer(typeof(SoundValue))]
    public class SoundValueDrawer : PropertyDrawer
    {
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        private static readonly float Spacing = EditorGUIUtility.standardVerticalSpacing;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            
            SerializedProperty isRangeProp = property.FindPropertyRelative("isRange");
            SerializedProperty valueProp = property.FindPropertyRelative("value");
            SerializedProperty minProp = property.FindPropertyRelative("min");
            SerializedProperty maxProp = property.FindPropertyRelative("max");

            Rect fieldRect = new Rect(
                position.x,
                position.y,
                position.width - LineHeight - Spacing, 
                position.height
            );
            Rect modeSelectorRect = new Rect(
                position.x + position.width - LineHeight,
                position.y,
                LineHeight, 
                position.height
            );
            
            float minLimit = 0;
            float maxLimit = 2;
                
            var rangeAttr = fieldInfo.GetCustomAttributes(typeof(SoundValueRangeAttribute), true);
            if (rangeAttr.Length > 0)
            {
                var attr = (SoundValueRangeAttribute)rangeAttr[0];
                minLimit = attr.min;
                maxLimit = attr.max;
            }
            
            if (!isRangeProp.boolValue)
            {
                float value = valueProp.floatValue;
                value = EditorGUI.Slider(fieldRect, label, value, minLimit, maxLimit);
                valueProp.floatValue = value;
            }
            else
            {
                Rect controlRect = EditorGUI.PrefixLabel(fieldRect, label);
                
                //cache and reset indentation
                int originalIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
    
                //calculate sizing for properties
                float fieldWidth = 50f;
                float spacing = 5f;
                float sliderWidth = controlRect.width - (fieldWidth * 2) - (spacing * 2f);
                
                //set field sizes for min, slider, and max properties
                Rect minFieldRect = new Rect(controlRect.x, controlRect.y, fieldWidth, LineHeight);
                Rect sliderRect = new Rect(minFieldRect.xMax + spacing, controlRect.y, sliderWidth, LineHeight);
                Rect maxFieldRect = new Rect(sliderRect.xMax + spacing, controlRect.y, fieldWidth, LineHeight);
                
                //get min value and display in float field rounded to 3 decimals
                float minValue = minProp.floatValue;
                minValue = EditorGUI.FloatField(minFieldRect, Mathf.Round(minValue * 1000f) / 1000f);
                
                float maxValue = maxProp.floatValue;

                //draw min/max slider
                EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, minLimit, maxLimit);
                
                maxValue = EditorGUI.FloatField(maxFieldRect, Mathf.Round(maxValue * 1000f) / 1000f);
                
                if (minValue > maxValue) 
                    minValue = maxValue;
                
                minProp.floatValue = minValue;
                maxProp.floatValue = maxValue;
                
                EditorGUI.indentLevel = originalIndentLevel;
            }

            GUIStyle selectorStyle = new GUIStyle(EditorStyles.iconButton)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter
            };

            GUIContent modeSelectorIcon = EditorGUIUtility.IconContent("Icon Dropdown");
            modeSelectorIcon.tooltip = "Switch between constant and random range";

            if (GUI.Button(modeSelectorRect, modeSelectorIcon, selectorStyle))
            {
                GenericMenu menu = new GenericMenu();
                
                menu.AddItem(new GUIContent("Constant"), !isRangeProp.boolValue, () =>
                {
                    isRangeProp.boolValue = false;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.AddItem(new GUIContent("Random Range"), isRangeProp.boolValue, () =>
                {
                    isRangeProp.boolValue = true;
                    property.serializedObject.ApplyModifiedProperties();
                });
                
                menu.ShowAsContext();
            }
            
            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => LineHeight;
    }
}