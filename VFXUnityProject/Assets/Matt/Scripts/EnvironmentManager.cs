using System;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class EnvironmentManager : MonoBehaviour
{
    [Serializable]
    public class PositionBlend
    {
        public string Name;
        public Transform Transform;
        public Vector3 LowPosition;
        public Vector3 HighPosition;
    }

    private readonly int _wavePositionProperty = Shader.PropertyToID("_WavePosition");
    private readonly int _choppinessProperty = Shader.PropertyToID("_Choppiness");
    private readonly int _minWaveSpeedProperty = Shader.PropertyToID("_MinSpeed");
    private readonly int _maxWaveSpeedProperty = Shader.PropertyToID("_MaxSpeed");

    [SerializeField] private Volume _globalVolume;
    private VolumeProfile _currentProfile;

    [SerializeField] private PositionBlend[] _positionBlends = {};
    public bool UpdateBlend = true;
    
    //---- Fog ----
    [Header("Fog")]
    public bool FogUpdatesEnabled = true;
    [SerializeField]
    private float _startingFogDensity = 10;
    [SerializeField]
    private Vector2 _fogClamp = new Vector2(10, 40);
    private Fog _fog;
    [SerializeField]
    private float _fogUpdateSpeed;
    [SerializeField] private AnimationCurve _fogDensityRamp = AnimationCurve.EaseInOut(0,0,1,1);
    
    [ReadOnly][SerializeField]
    private float _fogRatio;
    [ReadOnly][SerializeField]
    private float _rampedFogRatio;
    [ReadOnly][SerializeField]
    private float _desiredFogDensity;
    [ReadOnly][SerializeField]
    private float _currentFogDensity;

    
    //---- Chromatic Aberration ----
    [Header("Chromatic Aberration"), Space()]
    public bool ChromaticAberrationUpdatesEnabled = true;
    [SerializeField] private float _startingAberrationIntensity = 0;
    [SerializeField]
    private Vector2 _chromaticAberrationClamp = new Vector2(0, 1);
    [SerializeField] private float _aberrationUpdateSpeed = 0.1f;
    [SerializeField] private AnimationCurve _aberrationIntensityRamp = AnimationCurve.EaseInOut(0,0,1,1);
    
    private ChromaticAberration _chromaticAberration;

    [SerializeField][ReadOnly]
    private float _desiredAberrationIntensity;
    [SerializeField][ReadOnly]
    private float _currentAberrationIntensity;
    
    //---- Ocean ----
    [Header("Ocean"), Space()]
    public bool OceanChoppinessUpdatesEnabled = true;
    [SerializeField]
    private Renderer _oceanTile;
    private static Material _oceanMaterial;
    private float _wavePhase;
    private Vector2 _waveSpeedClamp = new Vector2(0, 1);
    [SerializeField]
    private float _waveSpeedScale = 1f;
    
    [SerializeField]
    private float _startingChoppiness = 0;
    public float OceanUpdateSpeed = 0.1f;
    public float ChoppinessModifier = 0;
    
    [SerializeField] private AnimationCurve _choppinessRamp = AnimationCurve.EaseInOut(0,0,1,1);
    [ReadOnly][SerializeField]
    private float _choppinessRatio;
    [ReadOnly][SerializeField]
    private float _rampedChoppinessRatio;
    
    [SerializeField][ReadOnly]
    private float _desiredOceanChoppiness;
    [SerializeField][ReadOnly]
    private float _currentOceanChoppiness;
    
    
    private float _duration;
    
    //---- Debug ----
    
    [ReadOnly]
    public float _waveSpeed;
    
    [ReadOnly]
    public float _waveSpeedModifier;
    
    private void Start()
    {
        _currentProfile = _globalVolume.profile;
        if (_currentProfile.TryGet(out _fog))
        {
            _currentFogDensity = _startingFogDensity;
            ApplyFogDensity(_startingFogDensity);
        }
        
        if (_currentProfile.TryGet(out _chromaticAberration))
        {
            _currentAberrationIntensity = _startingAberrationIntensity;
            ApplyAberrationIntensity(_startingAberrationIntensity);
        }
        
        _oceanMaterial = _oceanTile.sharedMaterial;
        _waveSpeedClamp = new Vector2
        {
            x = _oceanMaterial.GetFloat(_minWaveSpeedProperty),
            y = _oceanMaterial.GetFloat(_maxWaveSpeedProperty),
        };
        
        ApplyOceanValues(0, _startingChoppiness);
    }

    private void Update()
    {
        _duration += Time.deltaTime;
        
        UpdateOcean();
        UpdateFog();
        UpdateChromaticAberrationIntensity();
    }

    #region ----- Fog -----
    public void SetFogDensity(float ratio)
    {
        if (!FogUpdatesEnabled) return;
        
        _fogRatio = ratio;
        _rampedFogRatio = _fogDensityRamp.Evaluate(ratio);
        _desiredFogDensity = Mathf.Lerp(_fogClamp.x, _fogClamp.y, _rampedFogRatio);
    }
    
    private void UpdateFog()
    {
        _currentFogDensity = Mathf.Lerp(_currentFogDensity, _desiredFogDensity,
            Time.deltaTime * _fogUpdateSpeed);

        ApplyFogDensity(_currentFogDensity);
    }

    private void ApplyFogDensity(float value)
    {
        if (_fog != null)
        {
            _fog.meanFreePath.value = value;
        }
    }
    #endregion

    #region ---- Ocean ----
    public void SetOceanChoppiness(float ratio)
    {
        if (!OceanChoppinessUpdatesEnabled) return;
        
        _choppinessRatio = ratio;
        _rampedChoppinessRatio = _choppinessRamp.Evaluate(ratio + ratio * ChoppinessModifier);
        _desiredOceanChoppiness = _rampedChoppinessRatio;
    }

    private void UpdateOcean()
    {
        _currentOceanChoppiness = Mathf.Lerp(_currentOceanChoppiness, _desiredOceanChoppiness,
            Time.deltaTime * OceanUpdateSpeed);

        //Calculate Wave Texture Offset
        _waveSpeed = Mathf.Lerp(_waveSpeedClamp.x, _waveSpeedClamp.y, _currentOceanChoppiness);
        _waveSpeedModifier = _waveSpeed * _waveSpeedScale;
        _wavePhase += Time.deltaTime + _waveSpeedModifier;

        ApplyOceanValues(_wavePhase, _currentOceanChoppiness);
    }

    private void ApplyOceanValues(float wavePhase, float choppiness)
    {
        _oceanMaterial.SetFloat(_choppinessProperty, choppiness);
        _oceanMaterial.SetFloat(_wavePositionProperty, wavePhase);
        if (UpdateBlend)
        {
            Array.ForEach(_positionBlends, pb =>
            {
                pb.Transform.localPosition = Vector3.Lerp(pb.LowPosition, pb.HighPosition, _currentOceanChoppiness);
            });
        }
    }
    #endregion

    #region ---- Chromatic Aberration ----
    public void SetChromaticAberrationIntensity(float ratio)
    {
        if (!ChromaticAberrationUpdatesEnabled) return;
        
        float rampedIntensity = _aberrationIntensityRamp.Evaluate(ratio);
        _desiredAberrationIntensity = Mathf.Lerp(_chromaticAberrationClamp.x, _chromaticAberrationClamp.y, rampedIntensity);
    }
    
    private void UpdateChromaticAberrationIntensity()
    {
        _currentAberrationIntensity = Mathf.Lerp(_currentAberrationIntensity, _desiredAberrationIntensity,
            Time.deltaTime * _aberrationUpdateSpeed);

        ApplyAberrationIntensity(_currentAberrationIntensity);
    }
    
    private void ApplyAberrationIntensity(float intensity)
    {
        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.value = intensity;
        }
    }
    #endregion
}
