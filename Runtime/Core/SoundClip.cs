using UnityEngine;

namespace LoogaSoft.SoundSystem.Runtime
{
    [System.Serializable]
    public class SoundClip
    {
        public AudioClip clip;
        public float volume;
        public float pitch;
        public float delay;

        [SerializeField]
        private bool _initialized;

        public bool SoundEquals(SoundClip other)
        {
            if (other == null) return false;
            
            return clip == other.clip && 
                   Mathf.Approximately(volume, other.volume) && 
                   Mathf.Approximately(pitch, other.pitch) && 
                   Mathf.Approximately(delay, other.delay);
        }
    }
}