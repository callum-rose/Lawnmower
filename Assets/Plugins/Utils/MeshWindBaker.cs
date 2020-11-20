using UnityEngine;

namespace Utils
{
    internal class MeshWindBaker : MeshModifier
    {
        [SerializeField] private AnimationCurve windForYCurve;

        public override void ModifyMesh(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var colours = mesh.colors;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float wind = windForYCurve.Evaluate(vertex.y);

                Color colour = colours[i];
                colour = new Color(colour.r, colour.g, colour.b, wind);
                colours[i] = colour;
            }

            mesh.colors = colours;
        }
    }
}