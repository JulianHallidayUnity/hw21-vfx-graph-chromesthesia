using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// The RhythmAnalyzer is the main component that analyzes a song. 
    /// The RhythmAnalyzer component uses Analysis components to populate a RhythmData object with Tracks.
    /// </summary>
    [ExecuteInEditMode, AddComponentMenu("RhythmTool/Analyzer", -1)]
    public class RhythmAnalyzer : MonoBehaviour
    {
        /// <summary>
        /// Occurs when an initial part of the song has been analyzed.
        /// </summary>
        public event Action<RhythmData> Initialized;

        /// <summary>
        /// The RhythmData object for the song that is being analyzed.
        /// </summary>
        public RhythmData rhythmData { get; private set; }

        /// <summary>
        /// The current progress (0-1) of the analysis.
        /// </summary>
        public float progress { get; private set; }

        /// <summary>
        /// Is the analysis completed?
        /// </summary>
        public bool isDone { get; private set; }

        /// <summary>
        /// Has an initial length of the song been analyzed?
        /// </summary>
        public bool initialized { get; private set; }

        private AudioClip audioClip;

        private int hopSize = 1024;
        private int frameSize = 2048;
        private int bufferCount = 128;

        private int channels;
        private int sampleRate;

        private int totalFrames;
        private int lastFrame;

        private float[] buffer;

        private float[] window;

        private float[] samples;
        private float[] monoSamples;
        private float[] spectrum;
        private float[] magnitude;

        private Thread analyze;
        private AutoResetEvent waitForMainThread;

        private bool getData = false;
        private bool abort = false;

        private int initialLength;

        private List<Analysis> analyses = new List<Analysis>();

        /// <summary>
        /// Start analyzing an AudioClip.
        /// </summary>
        /// <param name="audioClip">The AudioClip to Analyze.</param>
        /// <param name="initialLength">An initial length in seconds to analyze before invoking the Initialized event.</param>
        /// <returns>RhythmData object containing analysis results.</returns>
        public RhythmData Analyze(AudioClip audioClip, float initialLength = 5)
        {
            Abort();

            this.audioClip = audioClip;
            this.initialLength = Mathf.RoundToInt(initialLength * audioClip.frequency / hopSize) - 1;

            Initialize();
            
            return rhythmData;
        }

        /// <summary>
        /// Abort the analysis process.
        /// </summary>
        public void Abort()
        {
            if (abort)
                return;

            if (analyze == null || !analyze.IsAlive)
                return;

            getData = false;
            abort = true;

            waitForMainThread.Set();
            analyze.Join();
        }

        private void Initialize()
        {
            abort = false;
            isDone = false;
            initialized = false;

            progress = 0;
            lastFrame = 0;
            totalFrames = audioClip.samples / hopSize;

            channels = audioClip.channels;
            sampleRate = audioClip.frequency;

            initialLength = Mathf.Min(initialLength, totalFrames);
            
            GetComponents(analyses);

            analyses.RemoveAll(a => !a.enabled);

            foreach (Analysis analysis in analyses)
                analysis.Initialize(sampleRate, frameSize, hopSize);

            rhythmData = RhythmData.Create(audioClip, analyses.Select(a => a.track));
                        
            StartAnalyze();
        }

        private void StartAnalyze()
        {
            int bufferSize = hopSize * bufferCount + (frameSize - hopSize);
            buffer = new float[bufferSize * channels];

            window = Util.HannWindow(frameSize);
            samples = new float[frameSize * channels];
            monoSamples = new float[frameSize];
            spectrum = new float[frameSize];
            magnitude = new float[frameSize / 2];

            waitForMainThread = new AutoResetEvent(false);
            analyze = new Thread(Analyze);
            analyze.Start();
        }
        
        private void Analyze()
        {
            while (lastFrame < totalFrames && !abort)
            {
                int index = lastFrame % bufferCount;

                if (index == 0)
                    FillBuffer();

                Array.Copy(buffer, index * hopSize * channels, samples, 0, samples.Length);

                ProcessFrame(samples);

                lastFrame++;

                progress = (float)lastFrame / totalFrames;
            }

            OnAnalysisDone();
        }

        private void OnAnalysisDone()
        {
            isDone = true;
        }

        private void ProcessFrame(float[] samples)
        {
            Util.GetMono(samples, monoSamples, channels);

            Array.Copy(monoSamples, spectrum, frameSize);

            Util.ApplyWindow(spectrum, window);
            Util.GetSpectrum(spectrum);
            Util.GetSpectrumMagnitude(spectrum, magnitude);
            
            foreach (Analysis analysis in analyses)
                analysis.Process(monoSamples, magnitude, lastFrame);            
        }

        private void FillBuffer()
        {
            getData = true;
            waitForMainThread.WaitOne();
        }

        private void GetData()
        {
            if (audioClip == null)
            {
                Abort();
                return;
            }

            getData = false;
            audioClip.GetData(buffer, lastFrame * hopSize);
            waitForMainThread.Set();
        }

        void Update()
        {
            if (getData)
                GetData();

            if(!initialized && lastFrame > initialLength)
            {
                initialized = true;

                if (Initialized != null)
                    Initialized(rhythmData);
            }
        }

#if UNITY_EDITOR
        void OnEnable()
        {
            UnityEditor.EditorApplication.update += Update;
        }

        void OnDisable()
        {
            UnityEditor.EditorApplication.update -= Update;
        }
#endif

    }
}