using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// RhythmData stores information about the song in the form of Tracks.
    /// </summary>
    [CreateAssetMenu(fileName = "Rhythm Data.asset", menuName = "RhythmTool/Rhythm Data")]
    public class RhythmData : ScriptableObject, IEnumerable<Track>
    {                
        /// <summary>
        /// The tracks contained in this RhythmData.
        /// </summary>
        public List<Track> tracks
        {
            get
            {
                return _tracks;
            }
        }

        /// <summary>
        /// The AudioClip associated with this RhythmData.
        /// </summary>
        public AudioClip audioClip;

        [SerializeField]
        private List<Track> _tracks = new List<Track>();

        /// <summary>
        /// Returns the first Track that stores Features of type T.
        /// </summary>
        /// <typeparam name="T">The Feature type of the Track.</typeparam>
        /// <returns>The first Track that stores Features of type T. Returns null if no Track could be found.</returns>
        public Track<T> GetTrack<T>() where T : Feature
        {
            foreach (Track track in _tracks)
            {
                if (track is Track<T>)
                    return track as Track<T>;
            }

            return null;
        }

        /// <summary>
        /// Returns the first Track that stores Features of type T and matches the name trackName.
        /// </summary>
        /// <typeparam name="T">The Feature type of the Track.</typeparam>
        /// <param name="trackName">The name of the track to look for.</param>
        /// <returns>the first Track that stores Features of type T and matches the name trackName. Returns null if no Track could be found.</returns>
        public Track<T> GetTrack<T>(string trackName) where T : Feature
        {
            foreach (Track track in _tracks)
            {
                if (track is Track<T> && track.name == trackName)
                    return track as Track<T>;
            }

            return null;
        }

        /// <summary>
        /// Finds all tracks that have Feature type T.
        /// </summary>
        /// <typeparam name="T">The Feature type of the Tracks.</typeparam>
        /// <param name="tracks">The list to populate with the Tracks.</param>
        public void GetTracks<T>(List<Track<T>> tracks) where T : Feature
        {
            foreach (Track track in tracks)
            {
                if (track is Track<T>)
                    tracks.Add(track as Track<T>);
            }
        }

        /// <summary>
        /// Finds all tracks that have Feature type T and name trackName.
        /// </summary>
        /// <typeparam name="T">The Feature type of the Tracks.</typeparam>
        /// <param name="tracks">The list to populate with the Tracks.</param>
        /// <param name="trackName">The name of the track to look for.</param>
        public void GetTracks<T>(List<Track<T>> tracks, string trackName) where T : Feature
        {
            foreach (Track track in tracks)
            {
                if (track is Track<T> && track.name == trackName)
                    tracks.Add(track as Track<T>);
            }
        }
        
        /// <summary>
        /// Finds all features of type T within a certain time frame.
        /// </summary>
        /// <typeparam name="T">The type of Features to look for.</typeparam>
        /// <param name="features">The list of Features to populate</param>
        /// <param name="start">The starting point in seconds.</param>
        /// <param name="end">The end point in seconds.</param>
        public void GetFeatures<T>(List<T> features, float start, float end) where T : Feature
        {
            foreach (Track track in _tracks)
            {
                if (track is Track<T>)
                    (track as Track<T>).GetFeatures(features, start, end);
            }
        }

        /// <summary>
        /// Finds all features of type T within tracks that match trackName within a certain time frame.
        /// </summary>
        /// <typeparam name="T">The type of Features to look for.</typeparam>
        /// <param name="features">The list of Features to populate</param>
        /// <param name="start">The starting point in seconds.</param>
        /// <param name="end">The end point in seconds.</param>
        /// <param name="trackName"></param>
        public void GetFeatures<T>(List<T> features, float start, float end, string trackName) where T : Feature
        {
            foreach (Track track in _tracks)
            {
                if (track.name == trackName && track is Track<T>)
                    (track as Track<T>).GetFeatures(features, start, end);
            }
        }

        /// <summary>
        /// Finds all features of type T within a certain time frame, including features with a length that intersects the time frame.
        /// </summary>
        /// <typeparam name="T">The type of Features to look for.</typeparam>
        /// <param name="features">The list of Features to populate</param>
        /// <param name="start">The starting point in seconds.</param>
        /// <param name="end">The end point in seconds.</param>
        public void GetIntersectingFeatures<T>(List<T> features, float start, float end) where T : Feature
        {
            foreach (Track track in _tracks)
            {
                if (track is Track<T>)
                    (track as Track<T>).GetIntersectingFeatures(features, start, end);
            }
        }

        /// <summary>
        /// Finds all features of type T within tracks that match trackName within a certain time frame, including features with a length that intersects the time frame.
        /// </summary>
        /// <typeparam name="T">The type of Features to look for.</typeparam>
        /// <param name="features">The list of Features to populate</param>
        /// <param name="start">The starting point in seconds.</param>
        /// <param name="end">The end point in seconds.</param>
        /// <param name="trackName"></param>
        public void GetIntersectingFeatures<T>(List<T> features, float start, float end, string trackName) where T : Feature
        {
            foreach (Track track in _tracks)
            {
                if (track.name == trackName && track is Track<T>)
                    (track as Track<T>).GetIntersectingFeatures(features, start, end);
            }
        }

        public IEnumerator<Track> GetEnumerator()
        {
            foreach (Track analysisData in _tracks)
                yield return analysisData;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tracks.GetEnumerator();
        }

        private void OnEnable()
        {
            tracks.RemoveAll(t => t == null);
        }

        void OnDestroy()
        {
            foreach (Track track in tracks)
            {
                if (Application.isPlaying)
                    Destroy(track);
                else
                    DestroyImmediate(track);
            }
        }

        /// <summary>
        /// Create a RhythmData object with a name and tracks.
        /// </summary>
        /// <param name="name">The name of the RhythmData object.</param>
        /// <param name="tracks">A collection of Tracks to add to the RhythmData object.</param>
        /// <returns>A new RhythmData object.</returns>
        public static RhythmData Create(AudioClip audioClip, IEnumerable<Track> tracks)
        {
            RhythmData data = CreateInstance<RhythmData>();

            data.audioClip = audioClip;
            data._tracks = new List<Track>(tracks);

            return data;
        }
    }
}