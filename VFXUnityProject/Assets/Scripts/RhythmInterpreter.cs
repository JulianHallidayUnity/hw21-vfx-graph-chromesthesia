using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RhythmTool;
using UnityEngine.Events;
using UnityEngine.VFX;

public class RhythmInterpreter : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] RhythmData rhythmData;
    [SerializeField] AudioSource audioSource;
    [SerializeField] VisualEffect vfx;
    [SerializeField] float maxVolume = 4f;
    [SerializeField] float maxOnsetStrength = 4f;
    [SerializeField] float onsetFade = 0.8f;

    [Header("Animator Control")]
    [SerializeField] bool useAnimator = false;
    [SerializeField] Animator playbackSpeedAnimator;
    [SerializeField] float playbackSpeed = 4f;

    [Header("Background Transform")]
    [SerializeField] bool useBackground = false;
    [SerializeField] Transform backgroundTransform;

    [Header("Rythm Events")]
    [SerializeField] private UnityEvent<float> _onBeatUpdate = new UnityEvent<float>();
    [SerializeField] private UnityEvent<float> _onVolumeUpdate = new UnityEvent<float>();
    [SerializeField] private UnityEvent<float> _onChromaUpdate = new UnityEvent<float>();
    [SerializeField] private UnityEvent<float> _onOnsetPowerUpdate = new UnityEvent<float>();
    
    
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
        float beatValue = beats.Count > 0 ? 1 : 0;
        vfx.SetFloat("BeatFloat", beatValue);
        _onBeatUpdate?.Invoke(beatValue);

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
        _onOnsetPowerUpdate?.Invoke(onsetPower);

        if (volumes.Count > 1)
        {
            float volumeSample = volumes[volumes.Count - 1].value;
            float volumeValue = Mathf.Clamp01(volumeSample / maxVolume);
            vfx.SetFloat("VolumeFloat", volumeValue);
            _onVolumeUpdate?.Invoke(volumeValue);
            if (useAnimator)
            {
                playbackSpeedAnimator.speed = volumeSample * playbackSpeed;
            }
        }

        // A new Chroma is added to the list every time a chroma occurs (the list is never cleared after Start)
        if (chromas.Count != chromaCount)
        {
            chromaCount = chromas.Count;

            // There are 11 notes in the Note enum, but does this plugin also deal in octaves?
            float normalizedNoteValue = Mathf.Clamp01((float)((int)chromas[chromaCount - 1].note) / 11f);
            vfx.SetFloat("ChromaFloat", normalizedNoteValue);
            _onChromaUpdate?.Invoke(normalizedNoteValue);
        }

        //Keep track of the previous playback time of the AudioSource.
        prevTime = time;

        if (useBackground)
        {
            vfx.SetVector3("BackgroundPosition", backgroundTransform.position);
            vfx.SetVector3("BackgroundRotation", backgroundTransform.eulerAngles);
        }
    }
}
