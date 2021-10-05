// Code to implement decently performing FFT for complex and real valued                                         
// signals. See www.lomont.org for a derivation of the relevant algorithms                                       
// from first principles. Copyright Chris Lomont 2010-2012.                                                      
// This code and any ports are free for all to use for any reason as long                                        
// as this header is left in place.                                                                              

using System;
using UnityEngine;

namespace RhythmTool
{
    public class LomontFFT
    {
        private float[] cosTable;
        private float[] sinTable;

        public void FFT(float[] data, bool forward)
        {
            int length = data.Length;

            if ((length & (length - 1)) != 0)
                throw new ArgumentException("data length " + length + " in FFT is not a power of 2");

            length /= 2;

            BitReverse(data);

            if (cosTable == null || cosTable.Length != length)
                InitializeTables(length);

            float sign = forward ? 1 : -1;

            int tPos = 0;

            for (int i = 2; i <= length; i *= 2)
            {
                for (int m = 0; m < i; m += 2)
                {
                    float wr = cosTable[tPos];
                    float wi = sign * sinTable[tPos];
                    tPos++;

                    for (int k = m; k < 2 * length; k += 2 * i)
                    {
                        int j = k + i;
                        float tempr = wr * data[j] - wi * data[j + 1];
                        float tempi = wi * data[j] + wr * data[j + 1];
                        data[j] = data[k] - tempr;
                        data[j + 1] = data[k + 1] - tempi;
                        data[k] = data[k] + tempr;
                        data[k + 1] = data[k + 1] + tempi;
                    }
                }
            }
        }

        public void RealFFT(float[] data, bool forward)
        {
            if (forward)
                FFT(data, true);

            Reconstruct(data, forward);

            if (forward)
            {
                float temp = data[0];
                data[0] += data[1];
                data[1] = temp - data[1];
            }
            else
            {
                float temp = data[0];
                data[0] = 0.5f * (temp + data[1]);
                data[1] = 0.5f * (temp - data[1]);

                FFT(data, false);
            }
        }

        private void Reconstruct(float[] data, bool forward)
        {
            int length = data.Length;

            float sign = forward ? 1 : -1;

            float c = 0.5f;
            float theta = Mathf.PI / (length / 2) * sign;

            float wtemp = Mathf.Sin(0.5f * theta);
            float wpr = -2 * wtemp * wtemp;
            float wpi = Mathf.Sin(theta);
            float wr = 1 + wpr;
            float wi = wpi;

            for (int i = 1; i < length / 4; i++)
            {
                int a = 2 * i;
                int b = length - 2 * i;
                float h1r = c * (data[a] + data[b]);
                float h1i = c * (data[a + 1] - data[b + 1]);
                float h2r = c * sign * (data[a + 1] + data[b + 1]);
                float h2i = c * -sign * (data[a] - data[b]);
                data[a] = h1r + wr * h2r - wi * h2i;
                data[a + 1] = h1i + wr * h2i + wi * h2r;
                data[b] = h1r - wr * h2r + wi * h2i;
                data[b + 1] = -h1i + wr * h2i + wi * h2r;
                wr = (wtemp = wr) * wpr - wi * wpi + wr;
                wi = wi * wpr + wtemp * wpi + wi;
            }
        }

        private void InitializeTables(int length)
        {
            cosTable = new float[length];
            sinTable = new float[length];

            int tPos = 0;

            for (int i = 2; i <= length; i *= 2)
            {
                float theta = Mathf.PI / (i / 2);
                float wr = 1, wi = 0;
                float wpi = Mathf.Sin(theta);
                float wpr = Mathf.Sin(theta / 2);

                wpr = -2 * wpr * wpr;

                for (int m = 0; m < i; m += 2)
                {
                    cosTable[tPos] = wr;
                    sinTable[tPos++] = wi;
                    float temp = wr;
                    wr = wr * wpr - wi * wpi + wr;
                    wi = wi * wpr + temp * wpi + wi;
                }
            }
        }

        private static void BitReverse(float[] data)
        {
            int length = data.Length;
            int mid = length >> 1;
            int j = 0;

            for (int i = 0; i < length - 1; i += 2)
            {
                if (i < j)
                {
                    Swap(data, i, j);
                    Swap(data, i + 1, j + 1);
                }

                int k = mid;

                while (k <= j)
                {
                    j -= k;
                    k >>= 1;
                }

                j += k;
            }
        }

        private static void Swap(float[] data, int a, int b)
        {
            float temp = data[a];

            data[a] = data[b];
            data[b] = temp;
        }
    }
}