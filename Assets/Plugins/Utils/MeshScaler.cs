using UnityEngine;

namespace Utils
{
    internal class MeshScaler : MeshModifier
    {
        [SerializeField] private float maxXZScaleVariation;
        [SerializeField] private float maxYScaleVariation;

        public override void ModifyMesh(Mesh mesh)
        {
            float GetRandomFactor(float max) => Random.Range(-max, max);
            float xz = GetRandomFactor(maxXZScaleVariation);
            Vector3 scale = Vector3.one + new Vector3(xz, GetRandomFactor(maxYScaleVariation), xz);

            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                vertex = new Vector3(vertex.x * scale.x, vertex.y * scale.y, vertex.z * scale.z);
                vertices[i] = vertex;
            }

            mesh.vertices = vertices;
        }
    }
}