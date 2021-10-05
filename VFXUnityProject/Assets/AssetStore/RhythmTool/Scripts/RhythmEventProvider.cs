using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// A RhythmEventProvider provides events for Tracks in a RhythmData object.
    /// </summary>
    [CreateAssetMenu(fileName = "Event Provider.asset", menuName = "RhythmTool/Event Provider")]
    public class RhythmEventProvider : RhythmTarget
    {
        [Range(-10, 10), Tooltip("The offset in seconds. E.g. an offset of 5 will trigger events 5 seconds in advance.")]
        public float offset;

        private Dictionary<int, RhythmEvent> _events = new Dictionary<int, RhythmEvent>();

        /// <summary>
        /// Process events based on A RhythmData object and a time frame.
        /// </summary>
        /// <param name="rhythmData">The RhythmData to process.</param>
        /// <param name="start">The start of the time frame in seconds.</param>
        /// <param name="end">The end of the time frame in seconds.</param>
        public override void Process(RhythmData rhythmData, float start, float end)
        {
            foreach (var rhythmEvent in _events)
                rhythmEvent.Value.Process(rhythmData, start + offset, end + offset);
        }

        /// <summary>
        /// Indicate that the playback time has been reset to a different time and process events for a RhythmData.
        /// </summary>
        /// <param name="rhythmData">The rhyRhythmData object.</param>
        /// <param name="time">The new playback time.</param>
        public override void Reset(RhythmData rhythmData, float time)
        {
            if(offset > 0)
                Process(rhythmData, time - offset, time);
        }

        /// <summary>
        /// Register a method to receive events for a specific Feature type.
        /// </summary>
        /// <typeparam name="T">The Feature type.</typeparam>
        /// <param name="action">The method.</param>
        public void Register<T>(Action<T> action) where T : Feature
        {
            Register(action, null);
        }

        /// <summary>
        /// Unregister a method to receive events for a specific Feature type.
        /// </summary>
        /// <typeparam name="T">The Feature type.</typeparam>
        /// <param name="action">The method.</param>
        public void Unregister<T>(Action<T> action) where T : Feature
        {
            Unregister(action, null);
        }

        /// <summary>
        /// Register a method to receive events for a specific Feature type and a specific Track name.
        /// </summary>
        /// <typeparam name="T">The Feature type.</typeparam>
        /// <param name="action">The method.</param>
        /// <param name="trackName">The Track name.</param>
        public void Register<T>(Action<T> action, string trackName) where T : Feature
        {
            int hash = GetHashCode(typeof(T), trackName);

            RhythmEvent<T> rhythmEvent;
            
            if (_events.ContainsKey(hash))
                rhythmEvent = _events[hash] as RhythmEvent<T>;
            else
            {                
                rhythmEvent = new RhythmEvent<T>(trackName);
                _events.Add(hash, rhythmEvent);
            }

            rhythmEvent.Register(action);
        }

        /// <summary>
        /// Unregister a method to receive events for a specific Feature type and a specific Track name.
        /// </summary>
        /// <typeparam name="T">The Feature type.</typeparam>
        /// <param name="action">The method.</param>
        /// <param name="trackName">The Track name.</param>
        public void Unregister<T>(Action<T> action, string trackName) where T : Feature
        {
            int hash = GetHashCode(typeof(T), trackName);

            RhythmEvent<T> rhythmEvent;

            if (_events.ContainsKey(hash))
                rhythmEvent = _events[hash] as RhythmEvent<T>;
            else
                return;

            rhythmEvent.Unregister(action);
        }

        void OnDestroy()
        {
            foreach(var rhythmEvent in _events.Values)
                rhythmEvent.Dispose();

            _events.Clear();
        }

        private static int GetHashCode(Type type, string trackName)
        {
            int hash = type.GetHashCode();

            if (!string.IsNullOrEmpty(trackName))
                hash = CombineHashCodes(hash, trackName.GetHashCode());

            return hash;
        }

        private static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }

        private abstract class RhythmEvent : IDisposable
        {
            public abstract void Process(RhythmData rhythmData, float start, float end);
            public abstract void Dispose();
        }

        private class RhythmEvent<T> : RhythmEvent where T : Feature
        {
            private Action<T> _action;

            private List<T> _features = new List<T>();

            private string trackName;

            public RhythmEvent(string trackName)
            {
                this.trackName = trackName;
            }
                        
            public override void Process(RhythmData rhythmData, float start, float end)
            {
                if (_action == null)
                    return;

                if (string.IsNullOrEmpty(trackName))
                    rhythmData.GetFeatures(_features, start, end);
                else
                    rhythmData.GetFeatures(_features, start, end, trackName);

                foreach (T feature in _features)
                    _action(feature);

                _features.Clear();
            }
            
            public void Register(Action<T> action)
            {
                _action = Delegate.Combine(_action, action) as Action<T>;
            }

            public void Unregister(Action<T> action)
            {
                _action = Delegate.Remove(_action, action) as Action<T>;
            }

            public override void Dispose()
            {
                _action = null;
            }        
        }        
    }
}