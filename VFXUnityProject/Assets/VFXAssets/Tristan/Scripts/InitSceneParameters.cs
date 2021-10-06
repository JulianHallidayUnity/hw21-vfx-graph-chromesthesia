using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitSceneParameters : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _cameraTarget;
    
    [SerializeField] private Light _mainLight;
    [SerializeField] private Light _lightTarget;

    private Vector3 storedCameraPosition;
    private Quaternion storedCameraRotation;

    private bool storedLightEnabled;

    private void OnEnable()
    {
        storedCameraPosition = _camera.transform.position;
        storedCameraRotation = _camera.transform.rotation;
        storedLightEnabled = _mainLight.gameObject.activeInHierarchy;
        
        _camera.transform.position = _cameraTarget.position;
        _camera.transform.rotation = _cameraTarget.rotation;
        _mainLight.gameObject.SetActive(_lightTarget.gameObject.activeInHierarchy);
    }

    private void OnDisable()
    {
        _camera.transform.position = storedCameraPosition;
        _camera.transform.rotation = storedCameraRotation;
        _mainLight.gameObject.SetActive(storedLightEnabled);
    }
}
