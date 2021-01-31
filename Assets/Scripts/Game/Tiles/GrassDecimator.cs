using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Tiles
{
    internal class GrassDecimator : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float ratio = 1;

        private GameObject[] _gameObjects;

        private void Awake()
        {
            GetGameObjects();

            Decimate();
        }

        private void OnValidate()
        {
            if (_gameObjects == null || _gameObjects.Length == 0)
            {
                GetGameObjects();
            }

            Decimate();
        }

        private void Decimate()
        {
            foreach (GameObject g in _gameObjects)
            {
                g.SetActive(true);
            }

            List<int> indicies = Enumerable.Range(0, _gameObjects.Length).ToList();
            int numToDecimate = Mathf.RoundToInt(_gameObjects.Length * (1 - ratio));
            for (int i = 0; i < numToDecimate; i++)
            {
                int randomIndex = Random.Range(0, indicies.Count);
                int objIndex = indicies[randomIndex];
                indicies.RemoveAt(randomIndex);

                _gameObjects[objIndex].SetActive(false);
            }
        }

        [Button]
        private void DestroyAllDeactivated()
        {
            Action<GameObject> destroyAction = Application.isPlaying ? (Action<GameObject>)Destroy : DestroyImmediate;

            foreach (GameObject obj in _gameObjects)
            {
                if (!obj.activeSelf)
                {
                    destroyAction(obj);
                }
            }

            GetGameObjects();
        }

        private void GetGameObjects()
        {
            int childCount = transform.childCount;
            _gameObjects = new GameObject[childCount];
            for (int i = 0; i < childCount; i++)
            {
                _gameObjects[i] = transform.GetChild(i).gameObject;
            }
        }
    }
}
