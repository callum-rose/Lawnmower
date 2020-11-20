using UnityEngine;

namespace Utils
{
    internal class MeshColourer : MeshModifier
    {
        [SerializeField] private Gradient gradient;
        [SerializeField] private Vector2 yRange;
        [SerializeField, Range(0, 1)] private float maxChannelVariation;

        public override void ModifyMesh(Mesh mesh)
        {
            float GetRandomFactor() => Random.Range(-maxChannelVariation, maxChannelVariation);
            Vector3 colourOffset = new Vector3(GetRandomFactor(), GetRandomFactor(), GetRandomFactor());

            var vertices = mesh.vertices;
            var colours = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float interpolent = Mathf.InverseLerp(yRange.x, yRange.y, vertex.y);
                Color colourForY = gradient.Evaluate(interpolent);
                colourForY += interpolent * (Color)(Vector4)colourOffset;
                colours[i] = colourForY;
            }

            mesh.colors = colours;
        }
    }
}