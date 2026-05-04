using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LoogaSoft.SoundSystem.Runtime
{
    public static class SoundSystem
    {
        //constants for sound system
        private const int START_POOL_SIZE = 32;
        private const float DURATION_PADDING = 0.05f;

        //internal state
        private static GameObject _rootObject;
        private static SoundTickManager _tickManager;
        private static readonly Queue<AudioSource> _sourcePool = new();
        private static readonly Dictionary<int, Queue<AudioSource>> _customPools = new();
        private static readonly Dictionary<int, int> _customPoolMap = new();
        private static readonly Dictionary<int, Transform> _customPoolParents = new();
        
        private static readonly Dictionary<int, Action> _completeCallbacks = new();
        
        //scratchpads to prevent allocations during runtime
        private static readonly List<int> _scratchIndices = new();
        private static readonly List<PlaybackRequest> _requestBatch = new();

        #region PlaybackRequest Struct
        /// <summary>
        /// Internal struct to transfer sound data from resolve logic to execute logic
        /// </summary>
        private readonly struct PlaybackRequest
        {
            public readonly AudioClip clip;
            public readonly float volume;
            public readonly float pitch;
            public readonly float delay;
            public readonly Vector3 position;
            public readonly Transform parent;
            public readonly AudioSource overrideSource;
            
            public PlaybackRequest(AudioClip clip, float volume, float pitch, float delay, Vector3 pos, Transform parent, AudioSource source = null)
            {
                this.clip = clip;
                this.volume = volume;
                this.pitch = pitch;
                this.delay = delay;
                position = pos;
                this.parent = parent;
                overrideSource = source;
            }
        }
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Plays a sound based on SoundData configuration (Random/Sequence/Simultaneous).
        /// Returns a SoundTask handle to control playback (Stop/Complete).
        /// </summary>
        /// <param name="data">SoundData configuration containing clips and playback logic</param>
        /// <param name="position">Position where the sound will be played</param>
        /// <param name="parent">(Optional) A transform that the sound will be attached to</param>
        /// <returns>A handle to the active sound task</returns>
        public static SoundTask Play(SoundData data, Vector3 position, Transform parent = null)
        {
            //convert sound data into playback instructions and execute
            if (ResolveRequests(data, position, parent, null, -1))
                return ExecuteBatch();
            return default;
        }

        public static SoundTask Play(SoundData data, AudioSource source, params SoundArgs[] args)
        {
            if (ResolveRequests(data, Vector3.zero, null, source, -1, args))
                return ExecuteBatch();
            
            return default;
        }
        /// <summary>
        /// Plays a raw SoundClip directly.
        /// </summary>
        /// <param name="clip">SoundClip configuration to play</param>
        /// <param name="position">Position where the sound will be played</param>
        /// <param name="parent">(Optional) A transform that the sound will be attached to</param>
        /// <returns>A handle to the active sound task</returns>
        public static SoundTask Play(SoundClip clip, Vector3 position, Transform parent = null)
        {
            //convert sound clip into playback instructions and execute
            if (ResolveSingleClip(clip, position, parent, null))
                return ExecuteBatch();
            return default;
        }
        /// <summary>
        /// Plays the sound at a specific index.
        /// </summary>
        /// <param name="data">SoundData configuration containing clips and playback logic</param>
        /// <param name="index">Index of sound clip that will be played</param>
        /// <param name="position">Position where the sound will be played</param>
        /// <param name="parent">(Optional) A transform that the sound will be attached to</param>
        /// <returns>A handle to the active sound task</returns>
        public static SoundTask PlayIndex(SoundData data, int index, Vector3 position, Transform parent = null)
        {
            //convert sound clip into playback instructions and execute
            if (ResolveRequests(data, position, parent, null, index))
                return ExecuteBatch();
            return default;
        }
        /// <summary>
        /// Used by Editor to preview sounds
        /// </summary>
        /// <param name="data">SoundData configuration to preview</param>
        /// <param name="source">Temporary AudioSource to play on</param>
        public static void PreviewSound(SoundData data, AudioSource source)
        {
            //convert sound clip into playback instructions and execute
            if (ResolveRequests(data, Vector3.zero, null, source, -1))
                ExecuteBatch();
        }
        /// <summary>
        /// Used by Editor to preview raw sound clip
        /// </summary>
        /// <param name="clip">SoundClip configuration to preview</param>
        /// <param name="source">Temporary AudioSource to play on</param>
        public static void PreviewSound(SoundClip clip, AudioSource source)
        {
            //convert sound clip into playback instructions and execute
            if (ResolveSingleClip(clip, Vector3.zero, null, source))
                ExecuteBatch();
        }
        
        /// <summary>
        /// Pre-allocates a custom pool of AudioSources based on a template audio source
        /// </summary>
        public static void CreatePool(AudioSource template, int size)
        {
            if (template == null) return;
            if (_tickManager == null) InitializeTickManager();

            int templateId = template.GetInstanceID();

            //create queue if it doesn't exist
            if (!_customPools.ContainsKey(templateId))
            {
                _customPools[templateId] = new Queue<AudioSource>();
                _customPoolParents[templateId] = template.transform.parent;
            }

            //populate queue
            for (int i = 0; i < size; i++)
            {
                AudioSource clone = CreateCustomSource(template, templateId);
                _customPools[templateId].Enqueue(clone);
            }
        }
        
        #endregion
        
        #region Private Methods
        #region Core Logic
        
        //convert sound clip configuration into request
        private static bool ResolveSingleClip(SoundClip clip, Vector3 pos, Transform parent, AudioSource overrideSource)
        {
            _requestBatch.Clear();
            if (!IsSoundClipValid(clip))
                return false;
            AddRequest(1f, 1f, 0f, 1f,  clip, 0, pos, parent, overrideSource);
            return true;
        }
        //convert sound data configuration into request
        private static bool ResolveRequests(SoundData data, Vector3 pos, Transform parent, AudioSource overrideSource, int overrideIndex, params SoundArgs[] args)
        {
            _requestBatch.Clear();
            if (!IsSoundDataValid(data))
                return false;
            
            float mVol = data.volume.GetValue();
            float mPitch = data.pitch.GetValue();
            float mDelay = data.delay.GetValue();
            float mPlaybackSpeed = data.playbackSpeed.GetValue();
            
            if (mPlaybackSpeed <= 0.01f)
                mPlaybackSpeed = 1f;

            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    SoundArgs arg = args[i];
                    float val = arg.IsRange ? Random.Range(arg.Value, arg.Max) : arg.Value;

                    switch (arg.Type)
                    {
                        case SoundArgs.ArgType.Volume: mVol = val; break;
                        case SoundArgs.ArgType.Pitch: mPitch = val; break;
                        case SoundArgs.ArgType.Delay: mDelay = val; break;
                        case SoundArgs.ArgType.PlaybackSpeed: mPlaybackSpeed = val; break;
                    }
                }
            }
            
            mPlaybackSpeed = Mathf.Max(0.01f, mPlaybackSpeed);

            //add explicit index request (overriding random/sequence logic)
            if (overrideIndex >= 0)
            {
                if (overrideIndex < data.soundClips.Length)
                    AddRequest(mVol, mPitch, mDelay, mPlaybackSpeed, data.soundClips[overrideIndex], 0, pos, parent, overrideSource);
                
                return _requestBatch.Count > 0;
            }

            //normal play types
            switch (data.playType)
            {
                case SoundPlayType.Random:
                    int pickedIndex = PickRandomIndexSmart(data);
                    AddRequest(mVol, mPitch, mDelay, mPlaybackSpeed, data.soundClips[pickedIndex], 0, pos, parent, overrideSource);
                    break;
                case SoundPlayType.Simultaneous:
                    //add all clips to request with no extra delay
                    foreach (var clip in data.soundClips)
                        AddRequest(mVol, mPitch, mDelay, mPlaybackSpeed, clip, 0, pos, parent, overrideSource);
                    break;
                case SoundPlayType.Sequence:
                    float totalDelay = 0f;
                    foreach (var clip in data.soundClips)
                    {
                        if (!IsSoundClipValid(clip)) 
                            continue;
                        
                        AddRequest(mVol, mPitch, mDelay, mPlaybackSpeed, clip, totalDelay, pos, parent, overrideSource);
                        
                        //calculate total duration of sequence based on clip pitch
                        float absPitch = Mathf.Abs(data.pitch.GetValue() * clip.pitch);
                        if (absPitch < 0.01f)
                            absPitch = 1f;
                        
                        float duration = clip.clip.length / absPitch;

                        totalDelay += duration;
                    }
                    break;
            }
            
            return _requestBatch.Count > 0;
        }
        
        private static void AddRequest(float masterVol, float masterPitch, float masterDelay, float playbackSpeed, SoundClip clipRef, float extraDelay, Vector3 pos, Transform parent, AudioSource overrideSource)
        {
            if (!IsSoundClipValid(clipRef)) 
                return;

            // Combine master settings + clip settings + sequential offset
            float finalVol = masterVol * clipRef.volume;
            float finalPitch = masterPitch * clipRef.pitch;
            float finalDelay = (masterDelay + clipRef.delay + extraDelay) / playbackSpeed;

            _requestBatch.Add(new PlaybackRequest(
                clipRef.clip, 
                finalVol, 
                finalPitch, 
                finalDelay, 
                pos, 
                parent, 
                overrideSource
            ));
        }
        
        private static SoundTask ExecuteBatch()
        {
            if (_tickManager == null)
                InitializeTickManager();
            
            //single sound doesn't need array allocation
            if (_requestBatch.Count == 1)
            {
                AudioSource source = GetSource(_requestBatch[0]);

                //capture task to track life cycle
                RegisterVoice(_requestBatch[0], source);
                return new SoundTask(source);
            }
            
            //multiple sounds need array allocation
            List<AudioSource> activeSources = new();
            for (int i = 0; i < _requestBatch.Count; i++)
            {
                AudioSource src = GetSource(_requestBatch[i]);
                activeSources.Add(src);
                RegisterVoice(_requestBatch[i], src);
            }

            return new SoundTask(activeSources);
        }

        private static void RegisterVoice(PlaybackRequest req, AudioSource source)
        {
            double startTime = AudioSettings.dspTime + req.delay;
            
            float absPitch = Mathf.Abs(req.pitch);
            if (absPitch < 0.01f) 
                absPitch = 1f;
            float duration = req.clip.length / absPitch;

            bool isPooled = true;
            if (req.overrideSource != null)
            {
                int templateId = req.overrideSource.GetInstanceID();
                isPooled = _customPools.ContainsKey(templateId);
            }
            
            _tickManager.AddVoice(new ActiveVoice
            {
                Source = source,
                Clip = req.clip,
                Volume = req.volume,
                Pitch = req.pitch,
                StartTime = startTime,
                EndTime = startTime + duration + DURATION_PADDING,
                IsPlaying = false,
                IsPooled = isPooled
            });
        }
        
        #endregion
        #region Utility Methods
        
        private static int PickRandomIndexSmart(SoundData data)
        {
            if (data.recentPlayedClipsIndices == null)
                data.recentPlayedClipsIndices = new();
            
            //ensure we don't exclude more clips than necessary
            int maxHistory = Mathf.Min(data.cyclesBeforeCanRepeat, data.soundClips.Length - 1);
            _scratchIndices.Clear();

            //get valid candidates (clips not in recent history)
            for (int i = 0; i < data.soundClips.Length; i++)
            {
                if (!data.recentPlayedClipsIndices.Contains(i))
                    _scratchIndices.Add(i);
            }

            //if no clips in history, reset
            if (_scratchIndices.Count == 0)
            {
                data.recentPlayedClipsIndices.Clear();
                for (int i = 0; i < data.soundClips.Length; i++)
                    _scratchIndices.Add(i);
            }
            
            //pick index and update history
            int pickedIndex = _scratchIndices[Random.Range(0, _scratchIndices.Count)];
            data.recentPlayedClipsIndices.Enqueue(pickedIndex);
            
            while (data.recentPlayedClipsIndices.Count > maxHistory)
                data.recentPlayedClipsIndices.Dequeue();
            
            return pickedIndex;
        }

        private static float GetClipDuration(AudioClip clip, float pitch)
        {
            if (clip == null) 
                return 0f;
            float p = Mathf.Abs(pitch);
            return clip.length / (p < 0.01f ? 1f : p);
        }
        private static bool IsSoundDataValid(SoundData data) => data != null && data.soundClips != null && data.soundClips.Length > 0;
        private static bool IsSoundClipValid(SoundClip clip) => clip != null && clip.clip != null;

        internal static void CancelTask(SoundTask task)
        {
            if (_tickManager != null)
                _tickManager.CancelTaskVoices(task);
        }

        internal static void ClearCallback(int instanceId)
        {
            if (_completeCallbacks.ContainsKey(instanceId))
                _completeCallbacks.Remove(instanceId);
        }
        internal static void RegisterCallback(AudioSource source, Action callback)
        {
            if (source == null || callback == null) 
                return;
            
            int id = source.GetInstanceID();
            
            if (_completeCallbacks.ContainsKey(id))
                _completeCallbacks[id] += callback;
            else
                _completeCallbacks[id] = callback;
        }
        
        #endregion
        #region Pooling
        
        private static AudioSource GetSource(PlaybackRequest req)
        {
            AudioSource source;

            if (req.overrideSource != null)
            {
                int templateId = req.overrideSource.GetInstanceID();

                //check if a custom pool exists for this override source
                if (_customPools.TryGetValue(templateId, out Queue<AudioSource> pool))
                {
                    source = pool.Count > 0 ? pool.Dequeue() : CreateCustomSource(req.overrideSource, templateId);
                    source.gameObject.SetActive(true);
                    source.mute = false;
                }
                else
                {
                    //legacy behavior (No custom pool was created, just use the single source)
                    source = req.overrideSource;
                }
            }
            else
            {
                //normal global pool
                source = GetPooledSource();
            }
    
            if (req.parent != null) 
                source.transform.SetParent(req.parent);
    
            source.transform.position = req.position;
            return source;
        }
        
        private static AudioSource GetPooledSource()
        {
            if (_tickManager == null)
                InitializeTickManager();

            AudioSource source = _sourcePool.Count > 0 ? _sourcePool.Dequeue() : CreateNewSource();
            source.gameObject.SetActive(true);

            source.mute = false;
            return source;
        }
        
        internal static void ReturnSource(AudioSource source)
        {
            if (source == null) return;
    
            source.Stop();
            source.clip = null;
            source.mute = true;

            int sourceId = source.GetInstanceID();

            //route source back to its specific custom pool or global pool
            if (_customPoolMap.TryGetValue(sourceId, out int templateId))
            {
                if (_customPools.TryGetValue(templateId, out Queue<AudioSource> pool))
                    pool.Enqueue(source);

                //check if template object still exists
                if (_customPoolParents.TryGetValue(templateId, out Transform customParent) && customParent != null)
                {
                    if (source.transform.parent != customParent)
                        source.transform.SetParent(customParent);
                }
                else
                {
                    //fallback if template was destroyed, send to global root
                    if (source.transform.parent != _rootObject.transform)
                        source.transform.SetParent(_rootObject.transform);
                }
            }
            else
            {
                //standard global source behavior
                _sourcePool.Enqueue(source);
                
                if (source.transform.parent != _rootObject.transform)
                    source.transform.SetParent(_rootObject.transform);
            }
        }

        private static AudioSource CreateNewSource()
        {
            GameObject obj = new GameObject("[Pooled Source]");
            obj.transform.SetParent(_rootObject.transform);
            
            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            
            return source;
        }

        private static AudioSource CreateCustomSource(AudioSource template, int templateId)
        {
            AudioSource clone =  Object.Instantiate(template, template.transform.parent);
            clone.gameObject.name = $"[Custom Pool] {template.gameObject.name}";
            clone.playOnAwake = false;
            clone.mute = true;

            _customPoolMap[clone.GetInstanceID()] = templateId;
            
            return clone;
        }
        
        #endregion
        #region Initialization
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _rootObject = null;
            _tickManager = null;
            _sourcePool.Clear();
            _customPools.Clear();
            _customPoolMap.Clear();
            _customPoolParents.Clear();
            _completeCallbacks.Clear();
            _scratchIndices.Clear();
            _requestBatch.Clear();
        }

        private static void InitializeTickManager()
        {
            var existingManager = Object.FindFirstObjectByType<SoundTickManager>();
            if (existingManager != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(existingManager.gameObject);
                else
                    Object.DestroyImmediate(existingManager.gameObject);
            }
            
            if (_rootObject == null)
            {
                _rootObject = new GameObject("[Sound System]");
                
                if (Application.isPlaying)
                    Object.DontDestroyOnLoad(_rootObject);
                else
                    _rootObject.hideFlags = HideFlags.HideAndDontSave;
            }
            
            _tickManager = _rootObject.AddComponent<SoundTickManager>();

            for (int i = 0; i < START_POOL_SIZE; i++)
                ReturnSource(CreateNewSource());
        }
        
        #endregion
        #endregion
        
    /// <summary>
    /// Lightweight struct to track playing sounds without allocation
    /// </summary>
    public struct ActiveVoice
    {
        public AudioSource Source;
        public double StartTime;
        public double EndTime;
        public bool IsPlaying;
        public bool IsPooled;
        
        public AudioClip Clip;
        public float Volume;
        public float Pitch;
    }
    
    [ExecuteAlways]
    public class SoundTickManager : MonoBehaviour
    {
        private readonly List<ActiveVoice> _voices = new(64);
        private readonly List<int> _indicesToRemove = new(16);
        
        public void AddVoice(ActiveVoice voice) => _voices.Add(voice);

        private void OnEnable()
        {
            #if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;
            #endif
        }

        private void OnDisable()
        {
            #if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            #endif
        }

        private void Update()
        {
            if (Application.isPlaying)
                ManualUpdate();
        }

        private void EditorUpdate()
        {
            if (!Application.isPlaying)
                ManualUpdate();
        }
        private void ManualUpdate()
        {
            if (_voices.Count == 0) 
                return;

            double currentTime = AudioSettings.dspTime;
            _indicesToRemove.Clear();
            
            for (int i = 0; i < _voices.Count; i++)
            {
                //get copy struct to modify
                ActiveVoice voice = _voices[i];

                //check if source destroyed externally
                if (voice.Source == null)
                {
                    _indicesToRemove.Add(i);
                    continue;
                }
                
                //handle start
                if (!voice.IsPlaying && currentTime >= voice.StartTime)
                {
                    if (!voice.Source.enabled)
                        voice.Source.enabled = true;

                    voice.Source.clip = voice.Clip;
                    voice.Source.volume = voice.Volume;
                    voice.Source.pitch = voice.Pitch;
                    voice.Source.Play();

                    voice.IsPlaying = true;
                    //update list with modified struct
                    _voices[i] = voice;
                }

                bool isFinished = false;
                
                if (voice.IsPlaying && currentTime >= voice.EndTime)
                    isFinished = true;
                else if (voice.IsPlaying && !voice.Source.isPlaying && currentTime > voice.StartTime + 0.1f)
                    isFinished = true;

                if (isFinished)
                {
                    int id = voice.Source.GetInstanceID();
                    if (_completeCallbacks.TryGetValue(id, out Action callback))
                    {
                        callback?.Invoke();
                        _completeCallbacks.Remove(id);
                    }
                    
                    if (voice.IsPooled)
                        ReturnSource(voice.Source);
                    
                    _indicesToRemove.Add(i);
                }
            }
            
            //cleanup voices
            for (int i = _indicesToRemove.Count - 1; i >= 0; i--)
            {
                int index = _indicesToRemove[i];
                
                int lastIndex = _voices.Count - 1;
                _voices[index] = _voices[lastIndex];
                _voices.RemoveAt(lastIndex);
            }
        }

        internal void CancelTaskVoices(SoundTask task)
        {
            //iterate backwards since we're removing elements
            for (int i = _voices.Count - 1; i >= 0; i--)
            {
                ActiveVoice voice = _voices[i];
                bool isMatch = false;
                
                //check if voice belongs to cancelled task
                if (task.MultipleSources != null)
                    isMatch = task.MultipleSources.Contains(voice.Source);
                else if (task.SingleSource != null)
                    isMatch = task.SingleSource == voice.Source;

                if (isMatch)
                {
                    int id = voice.Source.GetInstanceID();
                    
                    //clear callbacks so we don't end up calling OnComplete
                    ClearCallback(id);
                    
                    //stop source and return to pool
                    if (voice.IsPooled)
                        ReturnSource(voice.Source);
                    //stop if override source
                    else 
                        voice.Source.Stop();
                    
                    //remove from active tracking list
                    int lastIndex = _voices.Count - 1;
                    _voices[i] = _voices[lastIndex];
                    _voices.RemoveAt(lastIndex);
                }
            }
        }
    }
    }
 }