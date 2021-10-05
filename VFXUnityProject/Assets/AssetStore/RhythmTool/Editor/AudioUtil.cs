using UnityEngine;
using UnityEditor;

namespace RhythmTool
{
    public class AudioUtil
    {
        private static AudioSource audioSource;

        public static AudioClip audioCLip
        {
            get
            {
                return audioSource.clip;
            }
            set
            {
                audioSource.clip = value;
            }
        }

        public static bool isPlaying
        {
            get
            {
                return audioSource.isPlaying;
            }
        }

        public static float volume
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

        public static float time
        {
            get
            {
                return audioSource.time;
            }
            set
            {
                audioSource.time = value;
            }
        }

        static AudioUtil()
        {
            GameObject gameObject = GameObject.Find("RhythmTool_AudioPlayer");

            if (gameObject != null)
                audioSource = gameObject.GetComponent<AudioSource>();
            else
            {
                gameObject = EditorUtility.CreateGameObjectWithHideFlags("RhythmTool_AudioPlayer", HideFlags.HideAndDontSave);
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        public static void Play()
        {
            audioSource.Play();
        }

        public static void Stop()
        {
            audioSource.Stop();
        }    
        
        public static void Pause()
        {
            audioSource.Pause();
        }

        public static void UnPause()
        {
            audioSource.UnPause();
        }       
    }
}