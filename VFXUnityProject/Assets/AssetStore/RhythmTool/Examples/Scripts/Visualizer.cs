using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmTool.Examples
{
    public class Visualizer : MonoBehaviour
    {
        public RhythmAnalyzer analyzer;
        public RhythmPlayer player;
        public RhythmEventProvider eventProvider;

        public Text textBPM;

        public Line linePrefab;

        private List<Line> lines;

        private List<Chroma> chromaFeatures;

        private Note lastNote = Note.FSHARP;

        void Awake()
        {           
            analyzer.Initialized += OnInitialized;
            player.Reset += OnReset;

            eventProvider.Register<Beat>(OnBeat);
            eventProvider.Register<Onset>(OnOnset);
            eventProvider.Register<Value>(OnSegment, "Segments");

            lines = new List<Line>();

            chromaFeatures = new List<Chroma>();
        }
        
        void Update()
        {
            if (!player.isPlaying)
                return;

            UpdateLines();
        }
        
        private void UpdateLines()
        {
            float time = player.time;

            //Destroy all lines with a timestamp less than the current playback time.
            List<Line> toRemove = new List<Line>();
            foreach (Line line in lines)
            {
                if (line.timestamp < time || line.timestamp > time + eventProvider.offset)
                {
                    Destroy(line.gameObject);
                    toRemove.Add(line);
                }
            }

            foreach (Line line in toRemove)
                lines.Remove(line);

            //Update all Line positions based on their timestamp and current playback time, 
            //so they will move as the song plays.
            foreach (Line line in lines)
            {
                Vector3 position = line.transform.position;

                position.x = line.timestamp - time;

                line.transform.position = position;
            }
        }
                
        private void OnInitialized(RhythmData rhythmData)
        {
            //Start playing the song.
            player.Play();
        }

        private void OnReset()
        {
            //Destroy all lines when playback is reset.
            foreach (Line line in lines)
                Destroy(line.gameObject);

            lines.Clear();
        }

        private void OnBeat(Beat beat)
        {
            //Instantiate a line to represent the Beat.
            CreateLine(beat.timestamp, 0, 1, Color.black, 1);

            //Update BPM text.
            float bpm = Mathf.Round(beat.bpm * 10) / 10;
            textBPM.text = "(" + bpm + " BPM)";
        }

        private void OnOnset(Onset onset)
        {
            //Clear any previous Chroma features.
            chromaFeatures.Clear();

            //Find Chroma features that intersect the Onset's timestamp.
            player.rhythmData.GetIntersectingFeatures(chromaFeatures, onset.timestamp, onset.timestamp);

            //Instantiate a line to represent the Onset and Chroma feature.
            foreach (Chroma chroma in chromaFeatures)
                CreateLine(onset.timestamp, -2 + (float)chroma.note * .1f, .2f, Color.blue, onset.strength / 10);

            if (chromaFeatures.Count > 0)
                lastNote = chromaFeatures[chromaFeatures.Count - 1].note;

            //If no Chroma Feature was found, use the last known Chroma feature's note.
            if (chromaFeatures.Count == 0)
                CreateLine(onset.timestamp, -2 + (float)lastNote * .1f, .2f, Color.blue, onset.strength / 10);
        }

        private void OnSegment(Value segment)
        {
            //Instantiate a line to represent the segment.
            CreateLine(segment.timestamp, -3, 1, Color.green, segment.value / 10);
        }

        private void CreateLine(float timestamp, float position, float scale, Color color, float opacity)
        {
            Line line = Instantiate(linePrefab);
            line.transform.position = new Vector3(0, position, 0);
            line.transform.localScale = new Vector3(.1f, scale, .01f);

            line.Init(color, opacity, timestamp);

            lines.Add(line);
        }
    }
}