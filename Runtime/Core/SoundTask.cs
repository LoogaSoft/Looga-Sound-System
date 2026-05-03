using System.Collections.Generic;
using UnityEngine;

#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace LoogaSoft.SoundSystem.Runtime
{
    public readonly struct SoundTask
    {
        internal readonly AudioSource SingleSource;
        internal readonly List<AudioSource> MultipleSources;
        
        internal SoundTask(AudioSource source)
        {
            SingleSource = source;
            MultipleSources = null;
        }

        internal SoundTask(List<AudioSource> sources)
        {
            SingleSource = null;
            MultipleSources = sources;
        }

        public void Stop()
        {
            // if (MultipleSources == null) 
            //     return;
            // foreach (var source in MultipleSources)
            //     source?.Stop();
            SoundSystem.CancelTask(this);
        }

        public bool IsPlaying()
        {
            if (MultipleSources == null || MultipleSources.Count == 0) 
                return false;
            
            return MultipleSources.Exists(source => source.isPlaying); 
        }
    }
}