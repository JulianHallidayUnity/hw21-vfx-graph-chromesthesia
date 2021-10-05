using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// The DebugDrawer is a component that draws basic information for a RhythmPlayer.
    /// </summary>
    [RequireComponent(typeof(RhythmPlayer)), AddComponentMenu("RhythmTool/Debug Drawer", -2)]
    public class DebugDrawer : MonoBehaviour
    {
        /// <summary>
        /// The RhythmPlayer that is being drawn.
        /// </summary>
        public RhythmPlayer rhythmPlayer { get; private set; }

        private float width = 300;
        private float height = 75;
        private float padding = 10;

        void Awake()
        {
            rhythmPlayer = GetComponent<RhythmPlayer>();
        }

        void OnGUI()
        {
            if (rhythmPlayer.rhythmData == null)
                return;

            List<Track> tracks = rhythmPlayer.rhythmData.tracks;

            using (new GUI.GroupScope(new Rect(10, 10, width, (height + padding) * tracks.Count)))
            {
                Rect rect = new Rect(0, 0, width, height);

                for (int i = 0; i < tracks.Count; i++)
                {
                    using (new GUI.GroupScope(new Rect(0, i * (height + padding), width, height)))
                        TrackDrawer.Draw(tracks[i], rect, rhythmPlayer.time, rhythmPlayer.time + 6);
                }
            }            
        }
    }

    /// <summary>
    /// The TrackDrawer draws basic information for a specific Track.
    /// </summary>
    public abstract class TrackDrawer
    {
        private static Dictionary<Type, TrackDrawer> trackDrawers = new Dictionary<Type, TrackDrawer>();
        
        public static void Draw(Track track, Rect rect, float start, float end)
        {
            TrackDrawer trackDrawer = GetTrackDrawer(track);

            GUIStyle style = GUI.skin.box;
            style.alignment = TextAnchor.UpperLeft;

            GUI.Box(new Rect(0, 0, rect.width, rect.height), track.name, style);

            Rect trackRect = new Rect(5, 5, rect.width - 10, rect.height - 10);

            using(new GUI.GroupScope(trackRect))
                trackDrawer.DrawTrack(track, trackRect, start, end);
        }

        protected abstract void DrawTrack(Track track, Rect rect, float start, float end);

        public static TrackDrawer GetTrackDrawer(Track track)
        {
            Type type = track.GetType();

            TrackDrawer trackDrawer;

            if (trackDrawers.TryGetValue(type, out trackDrawer))
                return trackDrawer;

            trackDrawer = Bindings<TrackDrawer>.GetBinding(track);

            trackDrawers.Add(type, trackDrawer);

            return trackDrawer;
        }

        protected static float GetFeaturePosition(Feature feature, Rect rect, float start, float end)
        {
            return ((feature.timestamp - start) / (end - start)) * rect.width;
        }

        protected static void DrawRect(Rect position)
        {
            GUI.DrawTexture(position, Texture2D.whiteTexture);
        }       
    }

    /// <summary>
    /// The TrackDrawer draws basic information for a specific Track.
    /// </summary>
    /// <typeparam name="T">The type of Feature to draw.</typeparam>
    public class TrackDrawer<T> : TrackDrawer where T : Feature
    {
        private List<T> features;

        public TrackDrawer()
        {
            features = new List<T>();
        }

        protected override void DrawTrack(Track track, Rect rect, float start, float end)
        {
            DrawTrack(track as Track<T>, rect, start, end);
        }

        protected virtual void DrawTrack(Track<T> track, Rect rect, float start, float end)
        {
            features.Clear();

            track.GetIntersectingFeatures(features, start, end);

            foreach (T feature in features)
            {
                DrawFeature(feature, rect, start, end);
            }
        }

        protected virtual void DrawFeature(T feature, Rect rect, float start, float end)
        {
            float x = GetFeaturePosition(feature, rect, start, end);

            Rect featureRect = new Rect(x, rect.height, 1, -10);

            DrawRect(featureRect);
        }
    }

    public class OnsetDrawer : TrackDrawer<Onset>
    {
        protected override void DrawFeature(Onset feature, Rect rect, float start, float end)
        {
            float x = GetFeaturePosition(feature, rect, start, end);

            Rect featureRect = new Rect(x, rect.height, 1, -feature.strength * 10);

            DrawRect(featureRect);
        }
    }

    public class ChromaDrawer : TrackDrawer<Chroma>
    {
        protected override void DrawFeature(Chroma feature, Rect rect, float start, float end)
        {
            float x = GetFeaturePosition(feature, rect, start, end);
            float y = rect.height - 1 - (rect.height / 14) * (int)feature.note;

            float width = feature.length / (end - start) * rect.width;
            width = Mathf.Max(1, width);

            Rect featureRect = new Rect(x, y, width, -1);

            DrawRect(featureRect);
        }
    }

    public class ValueDrawer : TrackDrawer<Value>
    {
        protected override void DrawFeature(Value feature, Rect rect, float start, float end)
        {
            float x = GetFeaturePosition(feature, rect, start, end);

            Rect featureRect = new Rect(x, rect.height, 1, -feature.value * 10);

            DrawRect(featureRect);
        }
    }
}
