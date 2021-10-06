using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IridescentMaterialParameterTweaker : MonoBehaviour
{
    [SerializeField] float iridescentChangeRate = 0.01f;
    private Material iridescentMaterial;
    private float currentThickness = 0f;

    void Start()
    {
        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        iridescentMaterial = new Material(mr.sharedMaterial);
        mr.material = iridescentMaterial;
    }

    void Update()
    {
        AutoUpdateThickness();
    }

    private void AutoUpdateThickness()
    {
        currentThickness += Time.deltaTime * iridescentChangeRate;
        if (currentThickness > 1f)
        {
            currentThickness = 0f;
        }
        else if (currentThickness < 0f)
        {
            currentThickness = 1f;
        }
        SetIridescentThickness(currentThickness);
    }

    public void SetIridescentThickness(float thickness)
    {
        currentThickness = thickness;
        iridescentMaterial.SetFloat("_IridescenceThickness", currentThickness);
    }
}
