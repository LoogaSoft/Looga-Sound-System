using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoogaSoft.SoundSystem.Runtime
{
    [InitializeOnLoad]
    [SuppressMessage("Domain reload", "UDR0001:Domain Reload Analyzer")]
    public static class EditorSoundPreviewer
    {
        private static GameObject _hostObj;
        
        private static MethodInfo _audioUtilUpdateMethod;
        private static AudioClip _blankClip;

        static EditorSoundPreviewer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CleanupHostObj;
        }
        
        public static void PreviewSound(SoundData data)
        {
            if (data == null) 
                return;
            
            RunPreview(source => SoundSystem.PreviewSound(data, source));
        }

        public static void PreviewSound(SoundClip clip)
        {
            if (clip == null || clip.clip == null)  
                return;
            
            RunPreview(source => SoundSystem.PreviewSound(clip, source));
        }

        private static void RunPreview(Action<AudioSource> playAction)
        {
            WakeUpAudioEngine();

            try
            {
                if (_hostObj == null)
                {
                    _hostObj = new GameObject("[Editor Sound Preview Host]")
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
                
                AudioSource source = GetIdleSource();
                playAction(source);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Preview Error] {e.Message}");
            }
        }

        public static void StopPreview()
        {
            if (_hostObj == null)
                return;
            var sources = _hostObj.GetComponents<AudioSource>();
            foreach (var source in sources)
                source.Stop();
        }

        private static AudioSource GetIdleSource()
        {
            var sources = _hostObj.GetComponents<AudioSource>();
            foreach (var source in sources)
            {
                if (!source.isPlaying)
                    return source;
            }
            
            AudioSource newSrc = _hostObj.AddComponent<AudioSource>();
            newSrc.spatialBlend = 0f;
            return newSrc;
        }

        private static void CleanupHostObj()
        {
            EditorApplication.update -= WakeUpAudioEngine;
            
            if (_hostObj != null)
            {
                Object.DestroyImmediate(_hostObj);
                _hostObj = null;
            }
        }
        private static void WakeUpAudioEngine()
        {
            if (_blankClip == null)
                _blankClip = AudioClip.Create("BlankClip", 1, 1, 44100, false);

            if (_audioUtilUpdateMethod == null)
            {
                Assembly audioUtilAssembly = typeof(AudioImporter).Assembly;
                Type audioUtilClass = audioUtilAssembly.GetType("UnityEditor.AudioUtil");

                _audioUtilUpdateMethod = audioUtilClass.GetMethod(
                    "PlayPreviewClip",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                );
            }

            _audioUtilUpdateMethod?.Invoke(null, new object[] { _blankClip, 0, false });
        }
    }
}