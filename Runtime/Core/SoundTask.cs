using System.Collections.Generic;
using UnityEngine;

#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace LoogaSoft.SoundSystem.Runtime
{
    public struct SoundTask
    {
        private readonly List<AudioSource> _sources;
        
        internal List<AudioSource> Sources => _sources;
        
        public SoundTask(AudioSource source)
        {
            _sources = new List<AudioSource> { source };
        }

        public SoundTask(List<AudioSource> sources)
        {
            _sources = sources;
        }

        public void Stop()
        {
            if (_sources == null) 
                return;
            foreach (var source in _sources)
                source?.Stop();
        }

        public bool IsPlaying()
        {
            if (_sources == null || _sources.Count == 0) 
                return false;
            
            return _sources.Exists(source => source.isPlaying); 
        }
    }
}