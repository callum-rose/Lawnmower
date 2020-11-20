using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    class MeshCombiner : MonoBehaviour
    {
        [SerializeField] private GameObject[] gameObjects;
        [SerializeField, InlineProperty] private List<MeshModifier> modifiers;

        [Button]
        public void GetAllChildren()
        {
            gameObjects = (transform as IEnumerable).Cast<Transform>().Select(t => t.gameObject).ToArray();
        }

        [Button]
        public void Combine()
        {
            List<MeshFilter> meshFilters = new List<MeshFilter>();
            foreach (Transform child in gameObjects.Select(g => g.transform))
            {
                MeshFilter[] mf = child.GetComponentsInChildren<MeshFilter>();
                meshFilters.AddRange(mf);
            }

            CombineInstance[] combines = new CombineInstance[meshFilters.Count];

            for (int i = 0; i < meshFilters.Count; i++)
            {
                Mesh newMesh = Instantiate(meshFilters[i].sharedMesh);

                foreach (var mod in modifiers)
                {
                    mod.ModifyMesh(newMesh);
                }

                combines[i].mesh = newMesh;

                combines[i].transform = meshFilters[i].transform.parent.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);
            }

            GetComponent<MeshFilter>().sharedMesh = new Mesh();
            GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combines);

            if (GetComponent<MeshRenderer>().sharedMaterial == null)
            {
                GetComponent<MeshRenderer>().sharedMaterial = meshFilters[0].GetComponent<MeshRenderer>().sharedMaterial;
            }

            if (Application.isPlaying)
            {
                foreach (GameObject g in gameObjects)
                {
                    Destroy(g);
                }
            }
        }


        [Button]
        private void CombineAll()
        {
            var combiners = FindObjectsOfType<MeshCombiner>();
            foreach (var c in combiners)
            {
                c.Combine();
            }
        }

        [Button]
        private void ShareMesh()
        {
            var combiners = FindObjectsOfType<MeshCombiner>();

            var mesh = combiners[0].GetComponent<MeshFilter>().mesh;

            foreach (var c in combiners)
            {
                c.GetComponent<MeshFilter>().sharedMesh = mesh;
            }
        }
    }
}