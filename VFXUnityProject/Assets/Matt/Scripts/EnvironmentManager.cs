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
    [SerializeField]
    private float _startingFogDensity = 10;
    [SerializeField]
    private Vector2 _fogClamp = new Vector2(10, 40);
    private Fog _fog;
    
    [SerializeField]
    private float _fogUpdateSpeed;
    [SerializeField] private AnimationCurve _fogDensityRamp = AnimationCurve.EaseInOut(0,0,1,1);
    
    [ReadOnly]
    private float _fogRatio;
    [ReadOnly]
    private float _rampedFogRatio;
    [ReadOnly]
    private float _desiredFogDensity;
    [ReadOnly]
    private float _currentFogDensity;


    //---- Ocean ----
    [Header("Ocean"), Space(15)]
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
    [ReadOnly]
    public float _choppinessRatio;
    [ReadOnly]
    public float _rampedChoppinessRatio;
    
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
    }

    public void SetFogDensity(float ratio)
    {
        _fogRatio = ratio;
        _rampedFogRatio = _fogDensityRamp.Evaluate(ratio);
        _desiredFogDensity = Mathf.Lerp(_fogClamp.x, _fogClamp.y, _rampedFogRatio);
    }

    public void SetOceanChoppiness(float ratio)
    {
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
}
