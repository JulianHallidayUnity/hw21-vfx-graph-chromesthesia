using System;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// The Chromagram detects the most prominent notes at different times in the song.
    /// </summary>
    [AddComponentMenu("RhythmTool/Chromagram")]
    public class Chromagram : Analysis<Chroma>
    {
        public override string name
        {
            get
            {
                return "Chroma";
            }
        }
        
        private int startNote = 21;
        private int endNote = 89;

        private int bufferSize = 2048;
        private int downsampleFactor = 16;

        private int chromaInterval = 4;

        private int[] noteIndices;
        
        private float[] downsampled;

        private float[] spectrum;
        private float[] magnitude;

        private float[] window;

        private float[] pitchWindow;

        private float[] pitch;
        private float[] chroma;

        private int offset;

        private int[] chromaHistory;

        public override void Initialize(int sampleRate, int frameSize, int hopSize)
        {
            base.Initialize(sampleRate, frameSize, hopSize);

            noteIndices = new int[endNote - startNote];

            for (int i = 0; i < noteIndices.Length; i++)
            {
                float frequency = GetMidiFrequency(i + startNote);
                noteIndices[i] = FrequencyToIndex(frequency, bufferSize, sampleRate / downsampleFactor) - 1;
            }

            downsampled = new float[bufferSize];

            spectrum = new float[bufferSize];
            magnitude = new float[bufferSize / 2];

            window = Util.HannWindow(bufferSize);

            pitchWindow = new float[noteIndices.Length];

            for (int i = 0; i < noteIndices.Length; i++)
                pitchWindow[i] = Util.HannWindow(i, noteIndices.Length * 2) + .1f;
            
            pitch = new float[noteIndices.Length];
            chroma = new float[12];

            offset = (bufferSize * downsampleFactor) / hopSize / 2;
            
            chromaHistory = new int[12];
        }

        public override void Process(float[] samples, float[] magnitude, int frameIndex)
        {
            base.Process(samples, magnitude, frameIndex);

            Downsample(samples);

            if (frameIndex % chromaInterval == 0)
                UpdateChroma();            
        }

        private void Downsample(float[] samples)
        {
            int length = hopSize / downsampleFactor;
            int start = (frameSize - hopSize);

            for (int i = 0; i < bufferSize - length; i++)
                downsampled[i] = downsampled[i + length];
            
            for (int i = 0; i < length; i++)
            {
                float sum = 0;

                for (int j = 0; j < downsampleFactor; j++)
                    sum += samples[start + i * downsampleFactor + j];

                downsampled[bufferSize - length + i] = sum / downsampleFactor;
            }
        }

        private void UpdateChroma()
        {
            Array.Copy(downsampled, spectrum, bufferSize);

            Util.ApplyWindow(spectrum, window);
            Util.GetSpectrum(spectrum);
            Util.GetSpectrumMagnitude(spectrum, magnitude);

            for (int i = 0; i < pitch.Length; i++)
            {
                int index = noteIndices[i];
                int width = Mathf.FloorToInt(index * .015f);

                int start = Mathf.Max(index - width, 0);
                int end = Mathf.Min(index + width, magnitude.Length);

                float maxPitch = Util.Max(magnitude, start, end);

                pitch[i] = maxPitch * maxPitch * pitchWindow[i];
            }

            Array.Clear(chroma, 0, chroma.Length);

            for (int i = 0; i < pitch.Length; ++i)
                chroma[i % 12] += pitch[i];

            //float min = Util.Min(chroma);
            float max = Util.Max(chroma);
            float mean = Util.Mean(chroma);

            for (int i = 0; i < chroma.Length; i++)
                chroma[i] = (chroma[i] - mean) / (max - mean);
                //chroma[i] = (chroma[i] - min) / (mean - min);

            for (int i = 0; i < chroma.Length; i++)
            {
                if (chroma[i] >= .9f && chromaHistory[i] == 0)
                //if (chroma[i] >= 3.5f && chromaHistory[i] == 0)
                    chromaHistory[i] = frameIndex;

                if (chroma[i] < .8f && chromaHistory[i] != 0)
                //if (chroma[i] < 2.5f && chromaHistory[i] != 0)
                {
                    int index = chromaHistory[i];

                    if (frameIndex - index > 5)
                    {
                        Chroma chroma = new Chroma()
                        {
                            timestamp = FrameIndexToSeconds(index - offset),
                            length = FrameIndexToSeconds(frameIndex - index),
                            note = (Note)i
                        };

                        AddFeature(chroma);
                    }

                    chromaHistory[i] = 0;
                }
            }
        }

        private static int FrequencyToIndex(float frequency, int length, int samplerate)
        {
            return Mathf.RoundToInt(length * frequency / samplerate);
        }

        private static float GetMidiFrequency(int index)
        {
            return Mathf.Pow(2, (index - 69) / 12f) * 440;
        }
    }
}