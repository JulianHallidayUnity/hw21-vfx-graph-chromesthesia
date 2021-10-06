using System;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class EnvironmentManager : MonoBehaviour
{
    private static readonly int _wavePositionProperty = Shader.PropertyToID("_WavePosition");
    private static readonly int _choppinessProperty = Shader.PropertyToID("_Choppiness");
    private static readonly int _minWaveSpeedProperty = Shader.PropertyToID("_MinSpeed");
    private static readonly int _maxWaveSpeedProperty = Shader.PropertyToID("_MaxSpeed");

    [SerializeField] private Volume _globalVolume;
    private CloudLayer _clouds;
    
    [Serializable]
    public class PositionBlend
    {
        public string Name;
        public Transform Transform;
        public Vector3 LowPosition;
        public Vector3 HighPosition;
    }

    [SerializeField]
    private Renderer _oceanTile;
    
    public bool UpdateBlend = true;

    [SerializeField] private PositionBlend[] _positionBlends = {};

    private static Material _oceanMaterial;

    private float _wavePhase;
    private Vector2 _waveSpeedClamp = new Vector2(0, 1);
    [SerializeField]
    private float _waveSpeedScale = 1f;
    
    public float OceanUpdateSpeed = 0.1f;
    public float ChoppinessModifier = 0;
    
    [ReadOnly]
    public float _desiredOceanChoppiness;
    
    [ReadOnly]
    public float _currentOceanChoppiness;
    
    [ReadOnly]
    public float _waveSpeed;
    
    [ReadOnly]
    public float _waveSpeedModifier;
    
    private void Start()
    {
        _globalVolume.sharedProfile.TryGet(out _clouds);
        
        _oceanMaterial = _oceanTile.sharedMaterial;
        _waveSpeedClamp = new Vector2
        {
            x = _oceanMaterial.GetFloat(_minWaveSpeedProperty),
            y = _oceanMaterial.GetFloat(_maxWaveSpeedProperty),
        };
    }

    private void Update()
    {
        UpdateOcean();
    }

    public void SetCloudOpacity(float opacity)
    {
        if (_clouds != null)
        {
            _clouds.opacity.value = opacity;
        }
    }

    public void SetOceanChoppiness(float choppiness)
    {
        _desiredOceanChoppiness = choppiness + choppiness * ChoppinessModifier;
    }

    private void UpdateOcean()
    {
        _currentOceanChoppiness = Mathf.Lerp(_currentOceanChoppiness, _desiredOceanChoppiness,
            Time.deltaTime * OceanUpdateSpeed);
        _oceanMaterial.SetFloat(_choppinessProperty, _currentOceanChoppiness);

        _waveSpeed = Mathf.Lerp(_waveSpeedClamp.x, _waveSpeedClamp.y, _currentOceanChoppiness);
        _waveSpeedModifier = _waveSpeed * _waveSpeedScale;
        _wavePhase += Time.deltaTime + _waveSpeedModifier;
        _oceanMaterial.SetFloat(_wavePositionProperty, _wavePhase);
        
        if (UpdateBlend)
        {
            Array.ForEach(_positionBlends, pb =>
            {
                pb.Transform.localPosition = Vector3.Lerp(pb.LowPosition, pb.HighPosition, _currentOceanChoppiness);
            });
        }
    }
}
