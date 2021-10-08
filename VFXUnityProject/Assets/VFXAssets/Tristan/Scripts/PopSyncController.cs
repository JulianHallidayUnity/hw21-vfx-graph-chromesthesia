using System;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class PopSyncController : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Volume _globalVolume;

    [Header("Camera")]
    [SerializeField] private float _cameraBobAmplitude = 10f;
    [SerializeField] private float _cameraBobPeriod = 5f;
    [SerializeField] private float _cameraRotationMultiplier = 5f;

    [Header("DOF")]
    [SerializeField] private float _baseNearEnd = 0;
    [SerializeField] private float _nearEndRange = 2;
    [SerializeField] private float _baseFarEnd = 800;
    [SerializeField] private float _farEndRange = -400;
    [SerializeField] private float _dofUpdateSpeed;
   
    private VolumeProfile _currentProfile;
    private DepthOfField _dof;

    private float _currentNearEnd;
    private float _currentFarEnd;

    private Vector3 _cameraStartPos;
    private Quaternion _cameraStartRot;

    private float _audioVolume;
    private float _audioBeat;
    private float _audioOnset;
    private float _audioChroma;

    public void OnVolumeUpdate(float value)
    {
        _audioVolume = value;
    }

    public void OnBeatUpdate(float value)
    {
        _audioBeat = value;
    }

    public void OnOnsetUpdate(float value)
    {
        _audioOnset = value;
    }

    public void OnChromaUpdate(float value)
    {
        _audioChroma = value;
    }

    private void OnEnable()
    {
        _currentNearEnd = _baseNearEnd;
        _currentFarEnd = _baseFarEnd;

        _currentProfile = _globalVolume.profile;
        if (_currentProfile.TryGet(out _dof))
        {
            ApplyDOFSettings();
        }

        _cameraStartPos = _mainCamera.transform.position;
        _cameraStartRot = _mainCamera.transform.rotation;
    }

    private void Update()
    {
        UpdateCamera();
        UpdateDOF();
    }

    private void UpdateCamera()
    {
        float theta = Time.timeSinceLevelLoad / _cameraBobPeriod;
        float distance = _cameraBobAmplitude * Mathf.Sin(theta);

        _mainCamera.transform.position = _cameraStartPos + Vector3.up * distance;

        _mainCamera.transform.rotation = _cameraStartRot;
        _mainCamera.transform.Rotate(Vector3.up, _cameraRotationMultiplier * distance, Space.Self);
    }

    private void UpdateDOF()
    {
        float targetNear = _baseNearEnd + (_nearEndRange * (1 - (_audioVolume * _audioChroma)));

        _currentNearEnd = Mathf.Lerp(_currentNearEnd, targetNear, Time.deltaTime * _dofUpdateSpeed);

        float targetFar = _baseFarEnd + (_farEndRange * (1 - (_audioVolume * _audioChroma)));

        _currentFarEnd = Mathf.Lerp(_currentFarEnd, targetFar, Time.deltaTime * _dofUpdateSpeed);

        ApplyDOFSettings();
    }

    private void ApplyDOFSettings()
    {
        if (_dof != null)
        {
            _dof.nearFocusEnd.value = _currentNearEnd;
            _dof.farFocusEnd.value = _currentFarEnd;
        }
    }
}
