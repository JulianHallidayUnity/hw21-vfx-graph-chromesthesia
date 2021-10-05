using System.Collections.Generic;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// The analysis component processes data and populates a Track with Features.
    /// </summary>
    public abstract class Analysis : MonoBehaviour
    {
        /// <summary>
        /// The Track that contains the Features for this Analysis.
        /// </summary>
        public Track track
        {
            get
            {
                return GetTrack();
            }
        }

        /// <summary>
        /// The sample rate of the audio data.
        /// </summary>
        public int sampleRate { get; private set; }

        /// <summary>
        /// The number of samples in each frame.
        /// </summary>
        public int frameSize { get; private set; }

        /// <summary>
        /// The number of samples to move in between frames.
        /// </summary>
        public int hopSize { get; private set; }

        /// <summary>
        /// The name of the Analysis and the resulting Track.
        /// </summary>
        public abstract new string name { get; }

        /// <summary>
        /// The index of the current frame in the analysis process.
        /// </summary>
        protected int frameIndex { get; private set; }

        /// <summary>
        /// Initialize this Analysis for new audio data.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio data.</param>
        /// <param name="hopSize">The number of samples to move in between frames.</param>
        public virtual void Initialize(int sampleRate, int frameSize, int hopSize)
        {
            this.sampleRate = sampleRate;
            this.frameSize = frameSize;
            this.hopSize = hopSize;        
        }
        
        /// <summary>
        /// Process a frame of data.
        /// </summary>
        /// <param name="magnitude">A magnitude spectrum of a frame of audio data.</param>
        /// <param name="frameIndex">The index of a frame of audio data.</param>
        public virtual void Process(float[] samples, float[] magnitude, int frameIndex)
        {
            this.frameIndex = frameIndex;
        }

        /// <summary>
        /// Convert a frame index to a timestamp in seconds.
        /// </summary>
        /// <param name="frameIndex">A frame index.</param>
        /// <returns>The time in seconds corresponding to the frame index.</returns>
        protected float FrameIndexToSeconds(float frameIndex)
        {
            return frameIndex / ((float)sampleRate / hopSize);
        }

        protected abstract Track GetTrack();
    }

    /// <summary>
    /// The analysis component processes data and populates a track with Features.
    /// </summary>
    /// <typeparam name="T">The type of Feature this Analysis provides.</typeparam>
    [ExecuteInEditMode]
    public abstract class Analysis<T> : Analysis where T : Feature
    {
        public new Track<T> track { get; private set; }

        private Queue<T> toAdd = new Queue<T>();
        private Queue<T> toRemove = new Queue<T>();

        public override void Initialize(int sampleRate, int frameSize, int hopSize)
        {
            base.Initialize(sampleRate, frameSize, hopSize);

            track = Track<T>.Create(name);

            lock(toAdd)
                toAdd.Clear();

            lock(toRemove)
                toRemove.Clear();
        }

        protected sealed override Track GetTrack()
        {
            return track;
        }

        protected void AddFeature(T feature)
        {           
            lock(toAdd)
                toAdd.Enqueue(feature);
        }

        protected void RemoveFeature(T feature)
        {
            lock (toRemove)
                toRemove.Enqueue(feature);
        }

        void Update()
        {
            lock (toAdd)
            {
                while (toAdd.Count > 0)
                    track.Add(toAdd.Dequeue());
            }

            lock (toRemove)
            {
                while (toRemove.Count > 0)
                    track.Remove(toRemove.Dequeue());
            }
        }
    }
}