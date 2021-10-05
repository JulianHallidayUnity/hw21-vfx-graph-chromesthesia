using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RhythmTool
{
    public class TimeRuler
    {
        private float[] ticks = new float[]
        {
            .01f,
            .05f,
            .25f,
            .5f,
            1,
            5,
            15,
            30,
            60
        };

        private List<float>[] tickCache;

        private float start;
        private float end;

        public TimeRuler()
        {
            tickCache = new List<float>[ticks.Length];

            for (int i = 0; i < tickCache.Length; i++)
                tickCache[i] = new List<float>();
        }

        public void SetRange(float start, float end)
        {
            this.start = start;
            this.end = end;

            for (int i = 0; i < ticks.Length; i++)
            {
                tickCache[i].Clear();

                float tick = ticks[i];

                int tickStart = Mathf.FloorToInt(start / tick);
                int tickEnd = Mathf.CeilToInt(end / tick);
                
                for (int j = tickStart; j < tickEnd; j++)
                {
                    if (i < ticks.Length - 1 && j % Mathf.RoundToInt(ticks[i + 1] / tick) == 0)
                        continue;

                    float time = (j * tick);

                    tickCache[i].Add(time);
                }
            }
        }

        public void Draw(Rect rect)
        {
            float pixelsPerSecond = rect.width / (end - start);

            using (new GUI.GroupScope(rect))
            {
                for (int i = 0; i < ticks.Length; i++)
                {
                    float tick = ticks[i];
                    float pixelsPerTick = pixelsPerSecond * tick;

                    if (pixelsPerTick < 4)
                        continue;

                    float length = Mathf.Min(pixelsPerTick / 3, rect.height - 13);

                    Handles.color = Color.Lerp(Color.clear, Styles.rulerText.normal.textColor, .25f + pixelsPerTick / 75);

                    foreach (float time in tickCache[i])
                    {
                        float x = (time - start) * pixelsPerSecond;

                        Handles.DrawLine(new Vector3(x, rect.height), new Vector3(x, rect.height - length));

                        if (pixelsPerTick > 40)
                        {
                            Rect labelRect = new Rect(x + 2, -2, 40, 20);
                            GUI.Label(labelRect, FormatTime(time), Styles.rulerText);
                        }
                    }
                }
            }          
        }

        public void DrawMajorTicks(Rect rect)
        {
            float pixelsPerSecond = rect.width / (end - start);

            using (new GUI.GroupScope(rect))
            {
                for (int i = 0; i < ticks.Length; i++)
                {
                    float tick = ticks[i];
                    float pixelsPerTick = pixelsPerSecond * tick;

                    if (pixelsPerTick < 30)
                        continue;

                    float alpha = Mathf.Min((pixelsPerTick - 25) / 30, 1) * .2f;

                    Handles.color = new Color(0, 0, 0, alpha);

                    foreach (float time in tickCache[i])
                    {
                        float x = (time - start) * pixelsPerSecond;

                        Handles.DrawLine(new Vector3(x, rect.height), new Vector3(x, 0));
                    }
                }
            }            
        }

        private static string FormatTime(float time)
        {
            return string.Format("{0:F2}", time).Replace('.',':');
        }
    }
}