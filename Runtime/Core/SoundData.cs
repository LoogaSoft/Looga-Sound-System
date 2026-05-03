using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.SoundSystem.Runtime
{
    [CreateAssetMenu(fileName = "New Sound Data", menuName = "LoogaSoft/Sound Data")]
    public class SoundData : ScriptableObject
    {
        public SoundClip[] soundClips;
        
        [SoundValueRange(0f, 1f)]
        public SoundValue volume = new(1f);
        [SoundValueRange(-2f, 2f)]
        public SoundValue pitch = new(1f);
        [SoundValueRange(0f, 5f)]
        public SoundValue delay = new(0f);
        [SoundValueRange(0.01f, 10f)]
        public SoundValue playbackSpeed = new(1f);
        
        public SoundPlayType playType = SoundPlayType.Random;
        public SoundLoopType loopType = SoundLoopType.OneShot;
        
        public int loopCycles = 1;
        
        [Tooltip("Number of OTHER clips that must play before the current clip can repeat")]
        [Range(1, 10)]
        public int cyclesBeforeCanRepeat = 1;
        
        [System.NonSerialized]
        public Queue<int> recentPlayedClipsIndices = new();
    }
}