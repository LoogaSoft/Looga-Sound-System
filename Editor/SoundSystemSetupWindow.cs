using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LoogaSoft.SoundSystem.Editor
{
    public class SoundSystemSetupWindow : EditorWindow
    {
        private const string UniTaskDefineSymbol = "UNITASK_SUPPORT";
        private const string SetupCompleteKey = "LoogaSoundSystem_SetupComplete";

        private bool _isUniTaskSupported;

        [InitializeOnLoad]
        static class AutoSetupCompanion
        {
            static AutoSetupCompanion()
            {
                EditorApplication.delayCall += OnEditorLoad;
            }

            private static void OnEditorLoad()
            {
                if (EditorPrefs.GetBool(SetupCompleteKey, false)) 
                    return;
                
                EditorApplication.delayCall += OpenWindow;
            }
        }

        private void OnEnable()
        {
            _isUniTaskSupported = IsPackageDefined(UniTaskDefineSymbol);
        }
        private void OnDisable()
        {
            MarkSetupComplete();
        }
        
        [MenuItem("Window/LoogaSoft/Sound System/Configure Support")]
        public static void OpenWindow()
        {
            GetWindow<SoundSystemSetupWindow>("Support Configuration");
        }
        private static void MarkSetupComplete()
        {
            EditorPrefs.SetBool(SetupCompleteKey, true);
        }
        private void OnGUI()
        {
            GUILayout.Label("External Asset Support", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _isUniTaskSupported = EditorGUILayout.Toggle("Enable UniTask Support", _isUniTaskSupported);
            if (EditorGUI.EndChangeCheck())
            {
                if (_isUniTaskSupported) 
                    AddDefineSymbol(UniTaskDefineSymbol);
                else 
                    RemoveDefineSymbol(UniTaskDefineSymbol);
            }
            
            EditorGUILayout.Space();
            
            string status = $"UniTask Support: { (_isUniTaskSupported ? "Yes" : "No" ) }";
            EditorGUILayout.HelpBox(status, MessageType.Info);
        }

        private bool IsPackageDefined(string defineSymbol)
        {
            return PlayerSettings
                .GetScriptingDefineSymbols(GetNamedBuildTarget())
                .Contains(defineSymbol);
        }

        private NamedBuildTarget GetNamedBuildTarget()
        {
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            return NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(activeBuildTarget));
        }

        private void AddDefineSymbol(string defineSymbol)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(GetNamedBuildTarget());
            List<string> allDefines = defines.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();

            if (!allDefines.Contains(defineSymbol))
            {
                allDefines.Add(defineSymbol);
                ApplyDefineSymbols(allDefines);
            }
        }
        private void RemoveDefineSymbol(string defineSymbol)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(GetNamedBuildTarget());
            List<string> allDefines = defines.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();
            
            if (allDefines.Contains(defineSymbol))
            {
                allDefines.Remove(defineSymbol);
                ApplyDefineSymbols(allDefines);
            }
        }

        private void ApplyDefineSymbols(List<string> defineSymbols)
        {
            string newDefines = string.Join(";", defineSymbols.Distinct().ToArray());
            PlayerSettings.SetScriptingDefineSymbols(GetNamedBuildTarget(), newDefines);;
        }
    }
}