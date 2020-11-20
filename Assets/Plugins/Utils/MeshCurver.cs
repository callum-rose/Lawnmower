using UnityEngine;

namespace Utils
{
    internal class MeshCurver : MeshModifier
    {
        [SerializeField] private AnimationCurve curveOverY;

        public override void ModifyMesh(Mesh mesh)
        {
            float GetRandomFactor() => Random.Range(-curveOverY.Evaluate(1), curveOverY.Evaluate(1));
            Vector3 randomCurveScale = new Vector3(GetRandomFactor(), 0, GetRandomFactor());

            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float curveForY = curveOverY.Evaluate(vertex.y);
                vertex += curveForY * randomCurveScale;
                vertices[i] = vertex;
            }

            mesh.vertices = vertices;
        }
    }
}