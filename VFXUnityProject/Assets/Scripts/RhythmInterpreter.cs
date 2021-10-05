using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RhythmTool;
using UnityEngine.VFX;

public class RhythmInterpreter : MonoBehaviour
{
    [SerializeField] RhythmData rhythmData;
    [SerializeField] AudioSource audioSource;
    [SerializeField] VisualEffect vfx;

    [SerializeField] float maxVolume = 4f;
    [SerializeField] float maxOnsetStrength = 4f;
    [SerializeField] float onsetFade = 0.8f;

    private float prevTime;
    private int onsetCount = 0;
    private int chromaCount = 0;
    private float onsetPower = 0f;

    private List<Beat> beats = new List<Beat>();
    private List<Value> volumes = new List<Value>();
    private List<Onset> onsets = new List<Onset>();
    private List<Chroma> chromas = new List<Chroma>();
    //private List<Segmenter> segments = new List<Segmenter>();

    void Update()
    {
        //Get the current playback time of the AudioSource.
        float time = audioSource.time;

        //Clear the list.
        beats.Clear();

        //Find all beats for the part of the song that is currently playing.
        rhythmData.GetFeatures<Beat>(beats, prevTime, time);
        rhythmData.GetFeatures<Value>(volumes, prevTime, time);
        rhythmData.GetFeatures<Onset>(onsets, prevTime, time);
        rhythmData.GetFeatures<Chroma>(chromas, prevTime, time);
        //rhythmData.GetFeatures<Feature>(segments, prevTime, time);

        // Got beats this frame?
        if (beats.Count > 0)
        {
            vfx.SetFloat("BeatFloat", 1f);
        }
        else
        {
            vfx.SetFloat("BeatFloat", 0f);
        }

        // ------------------------- ONSET -------------------------
        // Decrease the value of the onsetPower every frame to fade out the 'hit'.
        // TODO: This should use Time.deltaTime.
        onsetPower *= onsetFade;

        // A new Onset is added to the list every time an onset occurs (the list is never cleared after Start)
        if (onsets.Count != onsetCount)
        {
            onsetCount = onsets.Count;

            // The the current onset is the last index, use its strength
            onsetPower = Mathf.Clamp01(onsets[onsetCount - 1].strength / maxOnsetStrength);
        }

        vfx.SetFloat("OnsetFloat", onsetPower);


        foreach (Value val in volumes)
        {
            vfx.SetFloat("VolumeFloat", Mathf.Clamp01(val.value / maxVolume));
        }

        // A new Chroma is added to the list every time a chroma occurs (the list is never cleared after Start)
        if (chromas.Count != chromaCount)
        {
            chromaCount = chromas.Count;

            // There are 11 notes in the Note enum, but does this plugin also deal in octaves?
            float normalizedNoteValue = Mathf.Clamp01((float)((int)chromas[chromaCount - 1].note) / 11f);
            vfx.SetFloat("ChromaFloat", normalizedNoteValue);
        }

        //Keep track of the previous playback time of the AudioSource.
        prevTime = time;
    }
}
