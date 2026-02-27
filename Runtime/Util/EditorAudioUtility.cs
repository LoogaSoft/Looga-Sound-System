#if UNITY_EDITOR
using System.Reflection;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEditor;

namespace LoogaSoft.SoundSystem.Runtime
{
    [SuppressMessage("Domain reload", "UDR0001:Domain Reload Analyzer")]
    public static class EditorAudioUtility
    {
        private static readonly MethodInfo PlayClipMethod;

        static EditorAudioUtility()
        {
            var audioUtilClass = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

            // Try finding the new method names (Unity 2020+)
            PlayClipMethod = audioUtilClass.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public);
            if (PlayClipMethod == null)
            {
                // Fallback for older method names (Pre-Unity 2020)
                PlayClipMethod = audioUtilClass.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public);
                ;
            }
        }

        // A wrapper function to play a clip using reflection
        public static void PlayClip(AudioClip clip)
        {
            if (PlayClipMethod != null)
            {
                // The method signature might vary by Unity version (e.g., takes startSample or not)
                // This example assumes 'clip' only
                try
                {
                    PlayClipMethod.Invoke(null, new object[] { clip, 0, false });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not invoke PlayClip method: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("PlayClip method not found via reflection.");
            }
        }
    }
#endif
}