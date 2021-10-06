using System;
using UnityEngine;

public class EnvironmentBlending : MonoBehaviour
{
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
    private static readonly int _choppiness = Shader.PropertyToID("_Choppiness");

    private void Start()
    {
        _oceanMaterial = _oceanTile.sharedMaterial;
    }

    private void Update()
    {
        float choppiness = _oceanMaterial.GetFloat(_choppiness);
        
        if (UpdateBlend)
        {
            Array.ForEach(_positionBlends, pb =>
            {
                pb.Transform.localPosition = Vector3.Lerp(pb.LowPosition, pb.HighPosition, choppiness);
            });
        }
    }
}
