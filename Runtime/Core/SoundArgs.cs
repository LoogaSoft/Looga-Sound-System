namespace LoogaSoft.SoundSystem.Runtime
{
    public readonly struct SoundArgs
    {
        internal enum ArgType
        {
            Volume,
            Pitch,
            Delay
        }

        internal readonly ArgType Type;
        internal readonly float Value;
        internal readonly float Max;
        internal readonly bool IsRange;

        private SoundArgs(ArgType type, float value, float max, bool isRange)
        {
            Type = type;
            Value = value;
            Max = max;
            IsRange = isRange;
        }
        
        public static SoundArgs Volume(float volume) => new(ArgType.Volume, volume, 1f, false);
        public static SoundArgs Volume(float min, float max) => new (ArgType.Volume, min, max, true);
        
        public static SoundArgs Pitch(float pitch) => new(ArgType.Pitch, pitch, 1f, false);
        public static SoundArgs Pitch(float min, float max) => new(ArgType.Pitch, min, max, true);

        public static SoundArgs Delay(float delay) => new(ArgType.Delay, delay, 0f, false);
        public static SoundArgs Delay(float min, float max) => new(ArgType.Delay, min, max, true);
    }
}