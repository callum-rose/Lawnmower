/// **********************
/// Written by Callum Rose
/// **********************

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils.Pool
{
    /// <summary>
    /// Pool class for GameObject. 
    /// Pool auto activates and deactivates GameObjects. 
    /// Pool should generally be set to unexpandable if it will contain different objects
    /// </summary>
    public class GameObjectPool
    {

        // List of items in pool
        public List<GameObject> ItemsInPool { get; private set; }
        // List of items that have been, but are not currently, in the pool
        public List<GameObject> ItemsOutsidePool { get; private set; }

        // Reference to initial object that will be cloned on pool expansion
        GameObject m_modelObj;

        // Number of objects the pool has instantiated
        int m_numCreated;

        // Parent transform to contain all pooled items
        Transform m_parentContainer;

        // Name of objects in pool
        string m_nameT;

        // How the pool will output objects
        PoolType m_type = PoolType.EXPANDABLE;

        // Main transform that all the pools will be a child of
        static Transform m_mainTranform;

        public static Transform MainTranform
        {
            get
            {
                if (m_mainTranform == null)
                    m_mainTranform = new GameObject("GameObject Pool Container").transform;
                return m_mainTranform;
            }
        }

        /// <summary>
        /// Gets the number of objects in the pool
        /// </summary>
        public int Size => ItemsInPool.Count;

        /// <summary>
        /// Initialises a new GameObjectPool reference
        /// </summary>
        public GameObjectPool(string name, GameObject obj)
        {
            //set expandable
            m_type = PoolType.EXPANDABLE;
            //save reference to gameobject
            m_modelObj = obj;

            Init(name, null);

            Put(obj);
        }

        public GameObjectPool(string name, GameObject obj, Transform parent)
        {
            //set expandable
            m_type = PoolType.EXPANDABLE;
            //save reference to gameobject
            m_modelObj = obj;

            Init(name, parent);

            Put(obj);
        }

        public GameObjectPool(string name, bool isRandom = true)
        {
            m_type = isRandom ? PoolType.RANDOM : PoolType.NORMAL;
            Init(name, null);
        }

        /// <summary>
        /// Put an item into the pool.
        /// </summary>
        public void Put(GameObject obj)
        {
            if (obj.scene.IsValid())
            {
                // Deactivate the attached gameobject
                obj.gameObject.SetActive(false);

                obj.transform.SetParent(m_parentContainer);
                // Put item in pool
                ItemsInPool.Add(obj);
                // Remove item from outside pool
                ItemsOutsidePool.Remove(obj);
            }
            else
            {
                Debug.LogWarning(string.Format("GameObject {0} put into {1} Pool is a prefab. I've instantiated it and put it in the pool :)", obj.name, m_nameT));

                Put(MonoBehaviour.Instantiate(obj));
            }
        }

        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        public GameObject Take()
        {
            GameObject gameObj;
            // If pool is empty consider creating a new object
            if (ItemsInPool.Count == 0)
            {
                // If pool is able to expand, clone and return the model object
                if (m_type == PoolType.EXPANDABLE)
                {
                    gameObj = UnityEngine.Object.Instantiate(m_modelObj.gameObject);
                    gameObj.name = m_nameT + " " + m_numCreated;
                    m_numCreated++;
                }
                else
                    throw new Exception("Pool is empty and is not set to be expandable.");
            }
            else
            {
                // Random index or zeroth
                int itemIndex = (m_type == PoolType.RANDOM ? UnityEngine.Random.Range(0, ItemsInPool.Count) : 0);
                // Take object out of pool and return it
                gameObj = ItemsInPool[itemIndex];
                // Remove item from the pool so it can't be retaken
                ItemsInPool.RemoveAt(itemIndex);

                // Take out of parent object
                gameObj.transform.SetParent(null);
            }

            // Activate object
            gameObj.SetActive(true);

            // Add to outside pool
            ItemsOutsidePool.Add(gameObj);

            return gameObj;
        }

        // Repool all objects
        public void RetrieveAll()
        {
            RetrieveAllBut(0);
        }

        public void RetrieveAllBut(int count)
        {
            if (ItemsOutsidePool.Count == 0)
                return;

            // Check for and remove duplicate references
            // Should never be different but just in case
            GameObject[] tempOutsidePool = ItemsOutsidePool.Distinct().ToArray();
            if (tempOutsidePool.Length != ItemsOutsidePool.Count)
                Debug.LogWarning((ItemsOutsidePool.Count - tempOutsidePool.Length) + " duplicate reference(s) in outsidePool for some reason.");

            for (int i = tempOutsidePool.Length - 1; i >= count; i--)
                Put(tempOutsidePool[i]);
        }

        void Init(string name, Transform parent)
        {
            ItemsInPool = new List<GameObject>();
            ItemsOutsidePool = new List<GameObject>();

            // create parent gameobject to hold pooled items
            m_parentContainer = new GameObject().transform;
            m_parentContainer.name = name + " Pool";

            if (parent)
            {
                // put parent into specified transform
                m_parentContainer.SetParent(parent);
            }
            else
            {
                // Put parent into main pool container
                m_parentContainer.SetParent(MainTranform);
            }

            m_nameT = name;
        }
    }
}