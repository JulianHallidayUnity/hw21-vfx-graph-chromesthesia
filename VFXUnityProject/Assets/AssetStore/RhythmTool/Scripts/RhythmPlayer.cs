using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// The RhythmPlayer syncs RhythmData with an AudioSource. 
    /// It provides more accurate time information than an AudioSource.
    /// </summary>
    [RequireComponent(typeof(AudioSource)), AddComponentMenu("RhythmTool/Rhythm Player", -3)]
    public class RhythmPlayer : MonoBehaviour
    {
        /// <summary>
        /// This event occurs when the AudioSource has reached the end of the AudioClip.
        /// </summary>
        public event Action SongEnded;

        /// <summary>
        /// This event occurs when the playback time of the AudioSource has been reset to another time.
        /// </summary>
        public event Action Reset;

        /// <summary>
        /// The AudioSource that is being used by this RhythmPlayer.
        /// </summary>
        public AudioSource audioSource { get; private set; }

        /// <summary>
        /// The AudioClip that is part of the RhythmData.
        /// </summary>
        public AudioClip audioClip
        {
            get
            {
                if (rhythmData == null)
                    return null;

                return rhythmData.audioClip;
            }          
        }

        /// <summary>
        /// The current playback position in seconds.
        /// </summary>
        public float time
        {
            get
            {
                return _time;
            }
            set
            {                
                _time = value;
                audioSource.time = _time;
            }
        }       

        /// <summary>
        /// The volume of the AudioSource.
        /// </summary>
        public float volume
        {
            get
            {
                return audioSource.volume;
            }
            set
            {
                audioSource.volume = value;
            }
        }

        /// <summary>
        /// The pitch of the AudioSource.
        /// </summary>
        public float pitch
        {
            get
            {
                return audioSource.pitch;
            }
            set
            {
                audioSource.pitch = value;
            }
        }

        /// <summary>
        /// Is the AudioSource playing right now?
        /// </summary>
        public bool isPlaying
        {
            get
            {
                return audioSource.isPlaying;
            }
        }

        /// <summary>
        /// The previous playback position in seconds.
        /// </summary>
        public float prevTime { get; private set; }

        /// <summary>
        /// The RhythmData object to use. 
        /// </summary>
        public RhythmData rhythmData;

        /// <summary>
        /// A list of RhythmTargets to target with a RhythmData and time information.
        /// </summary>
        public List<RhythmTarget> targets;
               
        private float _time;

        /// <summary>
        /// Play the AudioClip. 
        /// </summary>
        public void Play()
        {
            if (audioClip == null)
                return;

            audioSource.clip = audioClip;

            if (audioSource.time == 0)
                OnReset();

            audioSource.Play();
        }

        /// <summary>
        /// Stop playing the AudioClip.
        /// </summary>
        public void Stop()
        {
            audioSource.Stop();
        }

        /// <summary>
        /// Pause playback.
        /// </summary>
        public void Pause()
        {
            audioSource.Pause();
        }

        /// <summary>
        /// Unpause playback.
        /// </summary>
        public void UnPause()
        {
            audioSource.UnPause();
        }

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        void Update()
        {
            prevTime = _time;
            
            if (audioSource.isPlaying)
                _time = Mathf.Clamp(_time + Time.unscaledDeltaTime * audioSource.pitch, audioSource.time - .02f, audioSource.time + .02f);
            
            if (rhythmData == null || audioClip == null)
                return;

            if (audioSource.timeSamples == audioClip.samples)
                OnSongEnded();
                        
            if (Mathf.Abs(_time - prevTime) > .5f + Time.unscaledDeltaTime)
                OnReset();
            
            foreach (RhythmTarget target in targets)
            {
                if (target == null)
                    continue;

                target.Process(rhythmData, prevTime, _time);
            }
        }
        
        private void OnSongEnded()
        {   
            if (SongEnded != null)
                SongEnded();
        }

        private void OnReset()
        {
            _time = audioSource.time;
            prevTime = _time;
                        
            if (Reset != null)
                Reset();

            foreach (RhythmTarget target in targets)
            {
                if (target == null)
                    continue;

                target.Reset(rhythmData, time);
            }
        }
    }
}