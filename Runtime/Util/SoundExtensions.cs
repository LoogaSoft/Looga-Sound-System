using System;

namespace LoogaSoft.SoundSystem.Runtime
{
    public static class SoundExtensions
    {
        public static SoundTask OnComplete(this SoundTask soundTask, Action callback)
        {
            if (callback == null || soundTask.Sources == null || soundTask.Sources.Count == 0) 
                return soundTask;

            if (soundTask.Sources.Count == 1)
            {
                SoundSystem.RegisterCallback(soundTask.Sources[0], callback);
                return soundTask;
            }

            int pendingCount = soundTask.Sources.Count;

            Action countdownWrapper = () =>
            {
                pendingCount--;
                if (pendingCount == 0)
                    callback.Invoke();
            };
            
            foreach (var source in soundTask.Sources)
                SoundSystem.RegisterCallback(source, countdownWrapper);
            
            return soundTask;
        }
    }
}