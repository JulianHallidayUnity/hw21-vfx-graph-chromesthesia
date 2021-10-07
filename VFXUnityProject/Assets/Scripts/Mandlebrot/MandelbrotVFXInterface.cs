using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UI;

public class MandelbrotVFXInterface : MonoBehaviour
{
    [SerializeField] VisualEffect vfx;
    public RawImage image;

    void Update()
    {
        vfx.SetTexture("MandelbrotImage", image.texture);
    }
}
