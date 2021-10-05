using System;
using UnityEngine;
using UnityEditor;

namespace RhythmTool
{
    public class WaveformView : IDisposable
    {
        public Color backgroundColor = new Color(.192f, .192f, .192f);
        public Color waveFormColor = new Color(1, .549f, 0);

        private AudioClip audioClip;

        private int width;
        private int height;

        private float pixelsPerSecond;
        private float samplesPerPixel;

        private int startIndex;
        private int prevStartIndex;

        private int length;

        private int sampleRate;
        private int channels;

        private float[] buffer;
        private MinMax[] data;
        private Color[] colors;

        private Texture2D texture;

        public WaveformView()
        {
            texture = new Texture2D(100, 100, TextureFormat.ARGB32, false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
        }

        public void SetAudioClip(AudioClip audioClip)
        {
            this.audioClip = audioClip;

            if (audioClip == null)
                return;

            prevStartIndex = startIndex + width;

            sampleRate = audioClip.frequency;
            channels = audioClip.channels;            
        }

        public void Draw(Rect rect, float start, float length)
        {
            if (audioClip == null)
                return;

            width = (int)rect.width;
            height = (int)rect.height;

            pixelsPerSecond = width / length;
            
            float samplesPerPixel = sampleRate / pixelsPerSecond;

            startIndex = Mathf.RoundToInt((start) * pixelsPerSecond);
            this.length = Mathf.RoundToInt(audioClip.samples / samplesPerPixel);

            if (data == null || data.Length != width)
                data = new MinMax[width];

            if (this.samplesPerPixel != samplesPerPixel)
            {
                int bufferSize = Mathf.RoundToInt(samplesPerPixel * channels) + 2 * channels;
                buffer = new float[bufferSize];
                prevStartIndex = startIndex + width;
            }

            this.samplesPerPixel = samplesPerPixel;

            if (Mathf.Abs(prevStartIndex - startIndex) > width)
                prevStartIndex = startIndex + width;

            if(startIndex < prevStartIndex)
                GetMinMax(startIndex, prevStartIndex);
            else if(startIndex > prevStartIndex)
                GetMinMax(prevStartIndex + width, startIndex + width);

            if (startIndex != prevStartIndex)
                UpdateTexture();

            prevStartIndex = startIndex;

            EditorGUI.DrawPreviewTexture(rect, texture);
        }

        private void GetMinMax(int startIndex, int endIndex)
        {
            for(int i = startIndex; i < endIndex; i++)
            {
                if (i < 0 || i > length - 1)
                    continue;

                int offset = Mathf.RoundToInt(i * samplesPerPixel);

                audioClip.GetData(buffer, offset);

                float min = float.MaxValue;
                float max = float.MinValue;

                for(int j = 0; j < buffer.Length - 1; j += channels)
                {
                    float mean = 0;

                    for (int k = 0; k < channels; k++)
                        mean += buffer[j + k];

                    mean /= channels;

                    if (mean > max)
                        max = mean;

                    if (mean < min)
                        min = mean;
                }

                data[i % width] = new MinMax
                {
                    min = min,
                    max = max
                };
            }
        }

        private void UpdateTexture()
        {
            if (texture.width != width || texture.height != height)
            {
                colors = new Color[width * height];
                texture.Reinitialize(width, height);
            }

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                    colors[j * width + i] = backgroundColor;
            }

            float halfHeight = height / 2;

            for (int i = 0; i < width; i++)
            {
                int index = i + startIndex;

                if (index < 0 || index > length)
                    continue;

                index %= width;

                int min = (int)halfHeight - 1;
                int max = (int)halfHeight + 1;

                MinMax minMax = data[index];

                min += Mathf.CeilToInt(minMax.min * halfHeight);
                max += Mathf.FloorToInt(minMax.max * halfHeight);

                for (int j = min; j < max; j++)
                    colors[j * width + i] = waveFormColor;
            }

            texture.SetPixels(colors);

            texture.Apply();
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private struct MinMax
        {
            public float min;
            public float max;
        }
    }
}