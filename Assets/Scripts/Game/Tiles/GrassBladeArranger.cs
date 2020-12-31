using System.Collections;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Game.Tiles
{
    public class GrassBladeArranger : MonoBehaviour
    {
        [SerializeField] private GameObject[] grassPrefabs;
        [SerializeField] private Vector2Int resolution;
        [SerializeField] private Vector3 size;
        [SerializeField] private bool doRandomRotation;

        [SerializeField] private Transform[] grassTransforms;

        [Button]
        private void Create()
        {
            grassTransforms = new Transform[resolution.x * resolution.y];
            Utils.Loops.TwoD(resolution.x, resolution.y, (x, z) =>
            {
                int randomIndex = Random.Range(0, grassPrefabs.Length);
                Transform newGrass = (UnityEditor.PrefabUtility.InstantiatePrefab(grassPrefabs[randomIndex], transform) as GameObject).transform;

                float newX = ((x / (resolution.x - 1f)) - 0.5f) * size.x;
                float newZ = ((z / (resolution.y - 1f)) - 0.5f) * size.z;

                newGrass.localPosition = new Vector3(newX, 0, newZ);
                if (doRandomRotation)
                {
                    newGrass.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * newGrass.localRotation;
                }

                grassTransforms[x + resolution.x * z] = newGrass;
            });
        }

        [Button]
        private void ReArrange()
        {
            Utils.Loops.TwoD(resolution.x, resolution.y, (x, z) =>
            {
                Transform grassTrans = grassTransforms[x + resolution.x * z];

                float newX = ((x / (resolution.x - 1f)) - 0.5f) * size.x;
                float newZ = ((z / (resolution.y - 1f)) - 0.5f) * size.z;

                grassTrans.localPosition = new Vector3(newX, 0, newZ);
                if (doRandomRotation)
                {
                    grassTrans.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * grassTrans.localRotation;
                }
            });
        }

        [Button]
        private void DestroyAllChildren()
        {
            while(transform.childCount > 0)
            {
                var child = transform.GetChild(0);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
