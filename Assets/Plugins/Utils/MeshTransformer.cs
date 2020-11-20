using Sirenix.OdinInspector;
using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(MeshFilter))]
    public class MeshTransformer : MonoBehaviour
    {
        [SerializeField] private Vector3 offset;
        [SerializeField] private Vector3 rotation;

        [Button]
        private void ApplyTransform()
        {
            Matrix4x4 translateMatrix = Matrix4x4.Translate(offset);
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(rotation));
            Matrix4x4 transformationMatrix = rotationMatrix * translateMatrix;

            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                vertices[i] = transformationMatrix * vertices[i];
            }

            mesh.vertices = vertices;
        }
    }
}