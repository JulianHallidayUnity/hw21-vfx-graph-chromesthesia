using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class MandelbrotCS : MonoBehaviour
{
    double width, height;
    double rStart, iStart;
    int maxIterations, increment;
    float zoom;

    // Compute Shader Vars
    public ComputeShader shader;
    ComputeBuffer buffer;
    RenderTexture texture;
    public RawImage image;
    public RenderTexture renderTexture;
    public VisualEffect vfx;

    // Data for Compute Buffer
    public struct DataStruct
    {
        public double w, h, r, i;
        public int screenWidth, screenHeight;

    }

    DataStruct[] data;

    // Start is called before the first frame update
    void Start()
    {
        width = 4.5;
        height = width * Screen.height / Screen.width;
        rStart = -2.0;
        iStart = -1.25;
        maxIterations = 100;
        increment = 3;
        zoom = 0.5f;

        data = new DataStruct[1];

        data[0] = new DataStruct
        {
            w = width,
            h = height,
            r = rStart,
            i = iStart,
            screenWidth = Screen.width,
            screenHeight = Screen.height
        };

        buffer = new ComputeBuffer(data.Length, 40);
        texture = new RenderTexture(Screen.width, Screen.height, 0);
        texture.enableRandomWrite = true;
        texture.Create();

        Mandelbrot();
    }

    // Update is called once per frame
    void Update()
    {
        if(Mouse.current.leftButton.isPressed)
        {
            ZoomIn();
        }

        if (Mouse.current.rightButton.isPressed)
        {
            ZoomOut();
        }

        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            CenterScreen();
        }
    }

    void Mandelbrot()
    {
        int kernelHandle = shader.FindKernel("CSMain");

        buffer.SetData(data);
        shader.SetBuffer(kernelHandle, "buffer", buffer);

        shader.SetInt("maxIterations", maxIterations);
        shader.SetTexture(kernelHandle, "Result", texture);

        shader.Dispatch(kernelHandle, Screen.width / 24, Screen.height / 24, 1);

        RenderTexture.active = texture;
        image.material.mainTexture = texture;
        //vfx.SetTexture("MandelbrotImage", (Texture2D)texture);
        //Debug.Log("Width: " + texture.width + " Height: " + texture.height);
        //renderTexture = texture;
    }

    void CenterScreen()
    {
        rStart += (Mouse.current.position.ReadValue().x - (Screen.width / 2.0)) / Screen.width * width;
        iStart += (Mouse.current.position.ReadValue().y - (Screen.height / 2.0)) / Screen.height * height;

        data[0].r = rStart;
        data[0].i = iStart;

        Mandelbrot();
    }

    void ZoomIn()
    {
        maxIterations = Mathf.Max(100, maxIterations + increment);

        double wFactor = width * zoom * Time.deltaTime;
        double hFactor = height * zoom * Time.deltaTime;
        width -= wFactor;
        height -= hFactor;
        rStart += wFactor / 2.0;
        iStart += hFactor / 2.0;

        data[0].w = width;
        data[0].h = height;
        data[0].r = rStart;
        data[0].i = iStart;

        Mandelbrot();
    }

    void ZoomOut()
    {
        maxIterations = Mathf.Max(100, maxIterations - increment);

        double wFactor = width * zoom * Time.deltaTime;
        double hFactor = height * zoom * Time.deltaTime;
        width += wFactor;
        height += hFactor;
        rStart -= wFactor / 2.0;
        iStart -= hFactor / 2.0;

        data[0].w = width;
        data[0].h = height;
        data[0].r = rStart;
        data[0].i = iStart;

        Mandelbrot();
    }

    private void OnDestroy()
    {
        buffer.Dispose();
    }
}
