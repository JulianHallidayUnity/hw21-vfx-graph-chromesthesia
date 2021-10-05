using System;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// Contains general DSP utilities.
    /// </summary>
    public class Util
    {
        private static LomontFFT fft = new LomontFFT();

        /// <summary>
        /// Converts a signal with multiple interleaved channels to a mono signal.
        /// </summary>
        /// <param name="samples">The signal with multiple channels.</param>
        /// <param name="monoSamples">The converted signal.</param>
        /// <param name="channels">The number of channels.</param>
        public static void GetMono(float[] samples, float[] monoSamples, int channels = 0)
        {
            if (channels == 0)
                channels = samples.Length / monoSamples.Length;

            if (samples.Length % monoSamples.Length != 0)
                throw new ArgumentException("samples length is not a multiple of monoSamples length.");

            if (monoSamples.Length * channels != samples.Length)
                throw new ArgumentException("monoSamples length does not match samples length for " + channels + " channels");

            for (int i = 0; i < monoSamples.Length; i++)
            {
                float mean = 0;

                for (int j = 0; j < channels; j++)
                    mean += samples[i * channels + j];

                mean /= channels;

                monoSamples[i] = mean * 1.4f;
            }
        }

        /// <summary>
        /// Perform an in place FFT on a signal.
        /// </summary>
        /// <param name="samples">The signal.</param>
        public static void GetSpectrum(float[] samples)
        {
            fft.RealFFT(samples, true);
        }

        /// <summary>
        /// Get a magnitude spectrum from a complex frequency spectrum.
        /// </summary>
        /// <param name="spectrum">The complex frequency spectrum.</param>
        /// <param name="magnitude">The magnitude spectrum.</param>
        public static void GetSpectrumMagnitude(float[] spectrum, float[] magnitude)
        {
            if (magnitude.Length != spectrum.Length / 2)
                throw new Exception("magnitude length has to be half of spectrum length.");

            for (int i = 0; i < magnitude.Length - 2; i++)
            {
                int j = (i * 2) + 2;
                float re = spectrum[j];
                float im = spectrum[j + 1];

                magnitude[i] = Mathf.Sqrt((re * re) + (im * im));
            }

            magnitude[magnitude.Length - 2] = spectrum[0];
            magnitude[magnitude.Length - 1] = spectrum[1];
        }

        /// <summary>
        /// Get a phase spectrum from a complex frequency spectrum.
        /// </summary>
        /// <param name="spectrum">The complex frequency spectrum.</param>
        /// <param name="phase">The phase spectrum.</param>
        public static void GetSpectrumPhase(float[] spectrum, float[] phase)
        {
            if (phase.Length != spectrum.Length / 2)
                throw new Exception("phase length has to be half of spectrum length.");

            for (int i = 0; i < phase.Length - 2; i++)
            {
                int j = (i * 2) + 2;

                phase[i] = Mathf.Atan2(spectrum[j + 1], spectrum[j]);
            }

            phase[phase.Length - 2] = spectrum[0];
            phase[phase.Length - 1] = spectrum[1];
        }
        
        /// <summary>
        /// Apply a window to a signal.
        /// </summary>
        /// <param name="array">The signal.</param>
        /// <param name="window">The window.</param>
        internal static void ApplyWindow(float[] array, float[] window)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] *= window[i];
        }

        /// <summary>
        /// Calculate the mean of values of a section of an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="start">The start index.</param>
        /// <param name="end">The end index.</param>
        /// <returns>The mean of a section of the array.</returns>
        public static float Mean(float[] array, int start = 0, int end = 0)
        {
            if (end == 0)
                end = array.Length;

            float sum = 0;

            for (int i = start; i < end; i++)
                sum += array[i];

            return sum / (end - start);
        }
        
        /// <summary>
        /// Calculate a weighted sum of a section of an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="kernel">The kernel to use for the weighing.</param>
        /// <param name="index">The index to center the kernel on.</param>
        /// <returns>The weighted sum of a section of the array.</returns>
        public static float WeightedSum(float[] array, float[] kernel, int index)
        {
            float sum = 0;

            int start = index - kernel.Length / 2;
            int end = index + kernel.Length / 2;

            for (int i = start; i < end; i++)
            {
                if (i > 0 && i < array.Length)
                    sum += array[i] * kernel[i - start];
            }

            return sum;
        }

        /// <summary>
        /// Finds the index of the highest value in an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="start">The start of the section to look in.</param>
        /// <param name="end">The end of the section to look in.</param>
        /// <returns>The index of the highest value in the array.</returns>
        public static int MaxIndex(float[] array, int start = 0, int end = 0)
        {
            if (end == 0)
                end = array.Length;

            int index = start;

            for (int i = start; i < end; i++)
            {
                if (array[i] > array[index])
                    index = i;
            }

            return index;
        }

        /// <summary>
        /// Finds the index of the lowest value in an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="start">The start of the section to look in.</param>
        /// <param name="end">The end of the section to look in.</param>
        /// <returns>The index of the lowest value in the array.</returns>
        public static int MinIndex(float[] array, int start = 0, int end = 0)
        {
            if (end == 0)
                end = array.Length;

            int index = start;

            for (int i = start; i < end; i++)
            {
                if (array[i] < array[index])
                    index = i;
            }

            return index;
        }

        /// <summary>
        /// Finds the highest value in an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="start">The start of the section to look in.</param>
        /// <param name="end">The end of the section to look in.</param>
        /// <returns>The highest value in the array.</returns>
        public static float Max(float[] array, int start = 0, int end = 0)
        {
            return array[MaxIndex(array, start, end)];
        }

        /// <summary>
        /// Finds the lowest value in an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="start">The start of the section to look in.</param>
        /// <param name="end">The end of the section to look in.</param>
        /// <returns>The lowest value in the array.</returns>
        public static float Min(float[] array, int start = 0, int end = 0)
        {
            return array[MinIndex(array, start, end)];
        }
        
        /// <summary>
        /// Smooth an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="smoothedArray">The smoothed array.</param>
        /// <param name="kernel">The kernel to use for smoothing.</param>
        public static void Smooth(float[] array, float[] smoothedArray, float[] kernel)
        {
            for (int i = 0; i < array.Length; i++)
                smoothedArray[i] = WeightedSum(array, kernel, i) / kernel.Length;
        }
        
        /// <summary>
        /// Interpolate between values in an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The real valued index in the array.</param>
        /// <returns>The interpolated value of the array at the real valued index.</returns>
        public static float Interpolate(float[] array, float index)
        {
            int i = (int)index;

            if (i == array.Length - 1)
                return array[array.Length - 1];

            return array[i] + (array[i + 1] - array[i]) * (index - i);
        }

        /// <summary>
        /// Get a Hann window.
        /// </summary>
        /// <param name="array">The array to populate with the Hann window.</param>
        public static void HannWindow(float[] array)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = HannWindow(i, array.Length);
        }

        /// <summary>
        /// Get a Hann window.
        /// </summary>
        /// <param name="length">The length of the Hann window.</param>
        /// <returns>The Hann window.</returns>
        public static float[] HannWindow(int length)
        {
            float[] window = new float[length];

            HannWindow(window);

            return window;
        }

        /// <summary>
        /// Get the value of a Hann window with a length of windowSize at index n.
        /// </summary>
        /// <param name="n">The index in the Hann window.</param>
        /// <param name="windowSize">The length of the Hann window.</param>
        /// <returns></returns>
        public static float HannWindow(int n, int windowSize)
        {
            return 0.5f * (1 - Mathf.Cos((2 * Mathf.PI * n) / (windowSize - 1)));
        }
    }
}