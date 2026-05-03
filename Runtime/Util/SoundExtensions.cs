using System;

namespace LoogaSoft.SoundSystem.Runtime
{
    public static class SoundExtensions
    {
        public static SoundTask OnComplete(this SoundTask soundTask, Action callback)
        {
            if (callback == null) 
                return soundTask;

            //check if it's a single, unallocated source
            if (soundTask.SingleSource != null)
            {
                SoundSystem.RegisterCallback(soundTask.SingleSource, callback);
                return soundTask;
            }

            //check if it's a multi-source sequence/simultaneous play
            if (soundTask.MultipleSources != null && soundTask.MultipleSources.Count > 0)
            {
                int pendingCount = soundTask.MultipleSources.Count;

                Action countdownWrapper = () =>
                {
                    pendingCount--;
                    if (pendingCount == 0)
                        callback.Invoke();
                };
                
                foreach (var source in soundTask.MultipleSources)
                {
                    SoundSystem.RegisterCallback(source, countdownWrapper);
                }
            }

            //return the struct so we can continue chaining if needed
            return soundTask;
        }
    }
}