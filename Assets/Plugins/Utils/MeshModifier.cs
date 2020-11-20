using UnityEngine;

namespace Utils
{
    internal abstract class MeshModifier : MonoBehaviour
    {
        public abstract void ModifyMesh(Mesh mesh);
    }
}