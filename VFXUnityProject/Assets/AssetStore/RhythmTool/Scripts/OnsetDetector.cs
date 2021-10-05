using System;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// The OnsetDetector estimates times at which onsets occur in a song. 
    /// An onset is the start of a note in a song.
    /// </summary>
    [AddComponentMenu("RhythmTool/Onset Detector")]
    public class OnsetDetector : Analysis<Onset>
    {
        public override string name
        {
            get
            {
                return "Onsets";
            }
        }

        [Range(0, 1), Tooltip("Normalize the song. A higher value helps find onsets for quiet songs, but can increase false positives.")]
        public float normalization = .2f;
        
        [Range(0, 1), Tooltip("Threshold for finding onsets. A lower value will make the onset detection more sensitive, but can increase false positives.")]
        public float threshold = .3f;

        [Range(2, 32), Tooltip("The size of the buffer determines the minimum time between detected onsets and how much of the surrounding data is used for calculating the threshold.")]
        public int bufferSize = 12;

        private int start = 0;
        private int end = 1022;
                
        private float[] buffer;

        private float mean;
        private float m2;

        private float[] prevMagnitude;
        
        public override void Initialize(int sampleRate, int frameSize, int hopSize)
        {
            base.Initialize(sampleRate, frameSize, hopSize);

            buffer = new float[bufferSize];

            prevMagnitude = new float[frameSize / 2];

            mean = 1;
            m2 = 0;            
        }

        public override void Process(float[] samples, float[] magnitude, int frameIndex)
        {
            base.Process(samples, magnitude, frameIndex);
            
            float sample = SpectralDifference(magnitude);
            sample = Normalize(sample);
            buffer[frameIndex % bufferSize] = sample;
            
            int max = Util.MaxIndex(buffer);
            int current = (frameIndex - bufferSize / 2) % bufferSize;

            if (current == max)
            {
                float peak = buffer[max];
                float avg = Util.Mean(buffer, 0, bufferSize);

                if (peak > avg + threshold)
                {
                    Onset onset = new Onset()
                    {
                        timestamp = FrameIndexToSeconds(frameIndex - bufferSize / 2),
                        strength = peak
                    };

                    AddFeature(onset);
                }
            }
        }
        
        private float SpectralDifference(float[] magnitude)
        {
            float diff = 0;

            for (int i = start; i < end; i++)
            {
                float temp = Mathf.Abs(magnitude[i] * magnitude[i] - prevMagnitude[i] * prevMagnitude[i]);
                diff += Mathf.Sqrt(temp);
            }

            Array.Copy(magnitude, prevMagnitude, magnitude.Length);

            return diff / (end - start);
        }
        
        private float Normalize(float sample)
        {
            float delta = sample - mean;
            mean = mean + delta / (frameIndex + 1);
            m2 += delta * (sample - mean);

            float variance = m2 / (frameIndex + 1);
            float standardDeviation = Mathf.Sqrt(variance);

            if (standardDeviation == 0)
                return 0;

            return Mathf.Lerp(sample, (sample - mean) / standardDeviation, normalization);            
        }       
    }
}