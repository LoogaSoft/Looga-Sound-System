using UnityEngine;

namespace LoogaSoft.SoundSystem.Runtime
{
    public class SoundValueRangeAttribute : PropertyAttribute
    {
        public float min;
        public float max;
        
        public SoundValueRangeAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}