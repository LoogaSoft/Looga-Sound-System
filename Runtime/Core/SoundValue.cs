using UnityEngine;

namespace LoogaSoft.SoundSystem.Runtime
{
    [System.Serializable]
    public struct SoundValue
    {
        public bool isRange;
        public float value;
        public float min;
        public float max;

        public SoundValue(float value)
        {
            isRange = false;
            this.value = value;
            min = value;
            max = value;
        }
        public float GetValue() => isRange ? Random.Range(min, max) : value;
    }
}