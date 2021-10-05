using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// The Segmenter find sections of the song at which large changes in average volume occur.
    /// These changes often indicate different segments of a song.
    /// </summary>
    [AddComponentMenu("RhythmTool/Segmenter")]
    public class Segmenter : Analysis<Value>
    {
        public override string name
        {
            get
            {
                return "Segments";
            }
        }
        
        [Range(0, 64), Tooltip("The threshold for detecting large differences in volume.")]
        public float threshold = 22;

        [Range(1, 16), Tooltip("How much smoothing is applied to the audio signal.")]
        public int smoothing = 8;

        private Vector2 changeWeight = new Vector2(.1f, 10);

        private float changeStartSlope = .005f;
        private float changeEndSlope = .002f;

        private int iterations = 4;

        private int bufferSize;

        private float[][] buffer;
        private float[] kernel;

        private float w;

        private float current;
        private float next;
        
        private bool change;
        private float changeSign;
        private Vector2 changeStart;

        private float maxSlope;
        private int maxSlopeIndex;

        public override void Initialize(int sampleRate, int frameSize, int hopSize)
        {
            base.Initialize(sampleRate, frameSize, hopSize);

            bufferSize = smoothing * 16;

            buffer = new float[iterations][];

            for (int i = 0; i < iterations; i++)
                buffer[i] = new float[bufferSize];

            kernel = Util.HannWindow(bufferSize);

            w = 0;

            for (int i = 0; i < bufferSize; i++)
                w += kernel[i];
            
            maxSlope = 0;
            maxSlopeIndex = 0;
        }

        public override void Process(float[] samples, float[] magnitude, int frameIndex)
        {
            base.Process(samples, magnitude, frameIndex);

            float sample = Util.Mean(magnitude, 0, 350);
            
            for(int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < bufferSize - 1; j++)
                    buffer[i][j] = buffer[i][j + 1];

                if (i == 0)
                    buffer[i][bufferSize - 1] = sample;
                else
                    buffer[i][bufferSize - 1] = Util.WeightedSum(buffer[i - 1], kernel, bufferSize / 2) / w;
            }
            
            sample = Util.WeightedSum(buffer[iterations - 1], kernel, bufferSize / 2) / w;

            current = next;
            next = sample;
          
            FindSegments();
        }
                        
        private void FindSegments()
        {
            float slope = Mathf.Abs(next - current);

            if (slope > maxSlope)
            {
                maxSlope = slope;
                maxSlopeIndex = frameIndex - (bufferSize / 2) * iterations;
            }

            FindChangeEnd(slope);
            FindChangeStart(slope);
        }
        

        private void FindChangeEnd(float slope)
        {
            if (change && slope * changeSign < changeEndSlope)
            {
                float requiredLength = threshold;

                if (Mathf.Abs(slope) < changeStartSlope)
                    requiredLength *= .75f;
                
                Vector2 diff = new Vector2(frameIndex - (bufferSize / 2) * iterations, current) - changeStart;

                diff = Vector2.Scale(diff, changeWeight);

                if (diff.magnitude > requiredLength)
                {
                    Value segment = new Value()
                    {
                        timestamp = FrameIndexToSeconds(maxSlopeIndex),
                        value = current
                    };

                    AddFeature(segment);
                }

                change = false;
            }
        }

        private void FindChangeStart(float slope)
        {
            if (!change && Mathf.Abs(slope) > changeStartSlope)
            {
                maxSlope = slope;
                maxSlopeIndex = frameIndex - (bufferSize / 2) * iterations;

                changeStart = new Vector2(maxSlopeIndex, current);
                change = true;
                changeSign = Mathf.Sign(slope);
            }
        }        
    }
}