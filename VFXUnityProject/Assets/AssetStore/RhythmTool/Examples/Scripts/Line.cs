using UnityEngine;

namespace RhythmTool.Examples
{
    public class Line : MonoBehaviour
    {
        public float timestamp { get; private set; }

        public void Init(Color color, float opacity, float timestamp)
        {
            this.timestamp = timestamp;

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            color = Color.Lerp(Color.clear, color, opacity);
            meshRenderer.material.SetColor("_Color", color);
        }
    }
}