using System;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// The BeatTracker estimates at which points beats occur in the song. 
    /// The beat represents the rhythm of the song.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("RhythmTool/Beat Tracker")]
    public class BeatTracker : Analysis<Beat>
    {
        public override string name
        {
            get
            {
                return "Beats";
            }
        }

        private float[] signalBuffer;

        private float[] signal;
        private float[] smoothedSignal;

        private float[] autoCorrelation;
        private float[] combFilter;

        private float[] lengthScore;
        private float[] offsetScore;

        private float[] signalWindow;
        private float[] offsetWindow;
        private float[] kernel;

        private float[] prevMagnitude;
        private float prevSpectralFlux;

        private int maxBeatLength;
        private int minBeatLength;
        private int beatLength;
        private int prevBeatLength;

        private int beatOffset;
        private int updateOffset;

        private int bufferSize;

        private int resolution = 10;
        private int combElements = 8;

        public override void Initialize(int sampleRate, int frameSize, int hopSize)
        {
            base.Initialize(sampleRate, frameSize, hopSize);

            float framesPerMinute = (sampleRate * 60f) / hopSize;
            maxBeatLength = Mathf.RoundToInt(framesPerMinute / 80);
            minBeatLength = Mathf.RoundToInt(framesPerMinute / 160);

            bufferSize = maxBeatLength * combElements * 2;

            signalBuffer = new float[bufferSize];

            signal = new float[bufferSize];
            smoothedSignal = new float[bufferSize];

            autoCorrelation = new float[bufferSize];
            combFilter = new float[maxBeatLength * 2 * resolution];

            lengthScore = new float[maxBeatLength * resolution];
            offsetScore = new float[maxBeatLength * resolution];

            signalWindow = new float[bufferSize / 2];
            for (int i = 0; i < bufferSize / 2; i++)
                signalWindow[i] = Util.HannWindow(i, bufferSize);

            kernel = new float[8];
            for (int i = 0; i < kernel.Length; i++)
                kernel[i] = Util.HannWindow(i, kernel.Length);

            offsetWindow = new float[maxBeatLength * resolution];

            prevMagnitude = new float[frameSize / 2];
            prevSpectralFlux = 0;

            prevBeatLength = 0;
            beatLength = (minBeatLength + minBeatLength / 2) * resolution;
            updateOffset = maxBeatLength;
            beatOffset = -1;
        }

        public override void Process(float[] samples, float[] magnitude, int frameIndex)
        {
            base.Process(samples, magnitude, frameIndex);

            float sample = GetSample(magnitude);

            signalBuffer[frameIndex % bufferSize] = sample;

            beatOffset--;
            updateOffset--;

            if (updateOffset == 0)
            {
                UpdateSignal();
                UpdateLength();
                UpdateOffset();
            }

            if (beatOffset == 0)
            {
                Beat beat = new Beat()
                {
                    timestamp = FrameIndexToSeconds(frameIndex),
                    bpm = 60 / FrameIndexToSeconds((float)beatLength / resolution),
                };

                AddFeature(beat);
            }
        }

        private float GetSample(float[] magnitude)
        {
            float spectralFlux = 0;

            for (int i = 0; i < magnitude.Length; i++)
                spectralFlux += Mathf.Max(magnitude[i] - prevMagnitude[i], 0);

            Array.Copy(magnitude, prevMagnitude, magnitude.Length);

            float sample = spectralFlux - prevSpectralFlux;

            prevSpectralFlux = spectralFlux;

            return sample;
        }

        private void UpdateSignal()
        {
            for (int i = 0; i < bufferSize; i++)
                signal[i] = signalBuffer[(i + frameIndex + 1) % bufferSize];

            Array.Clear(signal, 0, 4);
            Array.Clear(signal, signal.Length - 4, 4);

            Util.Smooth(signal, smoothedSignal, kernel);

            for (int i = 0; i < signalWindow.Length; i++)
                smoothedSignal[i] *= signalWindow[i];
        }

        private void UpdateOffset()
        {
            float f = (float)beatLength / resolution;

            for (int i = 0; i < beatLength; i++)
            {
                float sum = 0;

                float offset = (float)i / resolution;
                offset = bufferSize - 1 - (f - offset);
                int n = Mathf.RoundToInt(offset / f);

                for (int j = 0; j < n; j++)
                    sum += Util.Interpolate(smoothedSignal, offset - j * f);
                    //sum += Util.Interpolate(signal, offset - j * f);

                float score = (sum / n) * offsetWindow[i];

                offsetScore[i] = Mathf.Lerp(offsetScore[i], score, .1f);
            }

            int max = Util.MaxIndex(offsetScore, 0, beatLength);

            beatOffset = Mathf.RoundToInt((float)max / resolution);
            updateOffset = beatOffset + Mathf.RoundToInt(beatLength / 2 / resolution);

            if (offsetScore[max] < .15f)
                beatOffset = -1;
        }

        private void UpdateLength()
        {
            UpdateAutoCorrelation();
            UpdateLengthScore();

            beatLength = Util.MaxIndex(lengthScore, minBeatLength * resolution);

            if (beatLength != prevBeatLength)
            {
                for (int i = 0; i < beatLength; i++)
                    offsetWindow[i] = .75f + Util.HannWindow(i, beatLength) * .25f;

                Array.Clear(offsetScore, beatLength, offsetScore.Length - beatLength);

                if ((float)Mathf.Abs(beatLength - prevBeatLength) / (minBeatLength * resolution) > .1f)
                    Array.Clear(offsetScore, 0, offsetScore.Length);
            }

            prevBeatLength = beatLength;

            //float max = Util.Max(lengthScore);
            //float min = Util.Min(lengthScore);
            //float mean = Util.Mean(lengthScore, minBeatLength * resolution, lengthScore.Length);

            //if ((mean - min) / (max - min) < .35f)
            //    beatLength = Util.MaxIndex(lengthScore, minBeatLength * resolution);
        }

        private void UpdateAutoCorrelation()
        {
            for (int i = minBeatLength / 2; i < autoCorrelation.Length; i++)
            {
                float sum = 0;

                for (int j = 0; j < smoothedSignal.Length - i; j++)
                    sum += smoothedSignal[j] * smoothedSignal[j + i];

                autoCorrelation[i] = sum / (smoothedSignal.Length - i);
            }

            float max = Util.Max(autoCorrelation, minBeatLength / 2);

            if (max < 1)
                return;

            for (int i = 0; i < autoCorrelation.Length; i++)
                autoCorrelation[i] /= max;
        }

        private void UpdateLengthScore()
        {
            for (int i = minBeatLength * resolution / 2; i < combFilter.Length - 1; i++)
            {
                float f = (float)i / resolution;

                float sum = 0;

                for (int j = 0; j < combElements; j++)
                    sum += Util.Interpolate(autoCorrelation, (j + 1) * f);

                combFilter[i] = sum / combElements;
            }

            for (int i = minBeatLength * resolution; i < lengthScore.Length; i++)
            {
                float score = combFilter[i] + combFilter[i / 2] + combFilter[i * 2];

                lengthScore[i] = Mathf.Lerp(lengthScore[i], score, .1f);
            }
        }
    }
}