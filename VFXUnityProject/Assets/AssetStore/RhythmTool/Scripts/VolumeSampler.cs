using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// The VolumeSampler samples volume at a certain sample rate.
    /// </summary>
    [AddComponentMenu("RhythmTool/Volume Sampler")]
    public class VolumeSampler : Analysis<Value>
    {
        public override string name
        {
            get
            {
                return "Volume";
            }
        }
        
        public int interval
        {
            get
            {
                return _interval;
            }
            set
            {
                _interval = Mathf.Clamp(value, 1, 64);
            }
        }
        
        public int smoothing
        {
            get
            {
                return _smoothing;
            }
            set
            {
                _smoothing = Mathf.Clamp(value, 0, 16);
            }
        }

        [SerializeField, Range(1, 64), Tooltip("How often to sample volume.")]
        private int _interval = 4;
        
        [SerializeField, Range(0, 16), Tooltip("How much smoothing is applied.")]
        private int _smoothing = 8;

        private int bufferSize;
        private int smoothingBufferSize;

        private float[] buffer;
        private float[] smoothingBuffer;

        private float[] smoothingKernel;

        private float w;

        public override void Initialize(int sampleRate, int frameSize, int hopSize)
        {
            base.Initialize(sampleRate, frameSize, hopSize);

            bufferSize = _interval;

            buffer = new float[bufferSize];

            if (_smoothing == 0)
            {
                smoothingBufferSize = 0;
            }
            else
            {
                smoothingBufferSize = _smoothing + 2;
                smoothingKernel = Util.HannWindow(smoothingBufferSize);
                smoothingBuffer = new float[smoothingBufferSize];

                w = 0;

                for (int i = 0; i < smoothingBufferSize; i++)
                    w += smoothingKernel[i];                
            }
        }

        public override void Process(float[] samples, float[] magnitude, int frameIndex)
        {
            base.Process(samples, magnitude, frameIndex);

            float mean = Util.Mean(magnitude, 0, magnitude.Length);

            int index = frameIndex % bufferSize;

            buffer[index] = mean;
            
            if (index == 0)
            {
                float value = Util.Mean(buffer, 0, bufferSize);

                if (smoothingBufferSize > 0)
                {
                    for (int i = 0; i < smoothingBufferSize - 1; i++)
                        smoothingBuffer[i] = smoothingBuffer[i + 1];

                    smoothingBuffer[smoothingBufferSize - 1] = value;
                    
                    value = Util.WeightedSum(smoothingBuffer, smoothingKernel, smoothingBufferSize / 2) / w;
                }

                Value volume = new Value()
                {
                    timestamp = FrameIndexToSeconds(frameIndex - (bufferSize * smoothingBufferSize) / 2),
                    value = value
                };

                AddFeature(volume);
            }
        }        
    }
}
