/// **********************
/// Written by CALLUM ROSE
/// **********************

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils.Pool
{
    /// <summary>
    /// Pool class for objects that inherit from Monobehaviour. Pool auto activates and deactivates GameObjects. 
    /// All objects inside must implement the IPoolable interface so the pool can perform initialise and reset actions.
    /// Pool should generally be set to unexpandable if it will contain different objects
    /// </summary>
    public class MonoBehaviourPool<T> where T : MonoBehaviour, IPoolable
    {
        // List of items in pool
        public List<T> pool;
        // List of items that have been, but are not currently, in the pool
        public List<T> outsidePool;

        // Reference to initial object that will be cloned on pool expansion
        T modelObj;
        // Number of objects the pool has instantiated
        int numCreated;
        // Parent transform to contain all pooled items
        Transform parent;
        // Name of objects in pool
        string nameT;

        // How the pool will output objects
        PoolType type = PoolType.EXPANDABLE;

        // Main transform that all the pools will be a child of
        static Transform mainTranform;

        public static Transform MainTranform
        {
            get
            {
                if (mainTranform == null)
                    mainTranform = new GameObject(typeof(T).Name + " Pool Container").transform;
                return mainTranform;
            }
        }

        /// <summary>
        /// Gets the number of objects in the pool
        /// </summary>
        public int Size
        {
            get { return pool.Count; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pool"/> class.
        /// </summary>
        public MonoBehaviourPool(string name, T obj)
        {
            //set expandable
            type = PoolType.EXPANDABLE;
            //save reference to gameobject
            this.modelObj = obj;

            Init(name);

            // If exists in the scene then add to the pool, otherwise it is just a file reference
            if (GameObject.Find(obj.name))
                Enpool(obj);
        }

        public MonoBehaviourPool(string name, bool isRandom = true)
        {
            type = isRandom ? PoolType.RANDOM : PoolType.NORMAL;
            Init(name);
        }

        void Init(string name)
        {
            pool = new List<T>();
            outsidePool = new List<T>();

            parent = new GameObject().transform;
            parent.name = name;

            // Put parent into main
            parent.SetParent(MainTranform);

            // nameT is the first word in name
            nameT = name.Split(' ')[0];
            // So that the pool is saved between scenes
            //MonoBehaviour.DontDestroyOnLoad(parent);
        }

        /// <summary>
        /// Put an item into the pool.
        /// </summary>
        public void Enpool(T obj)
        {
            obj.transform.SetParent(parent);
            // Put item in pool
            pool.Add(obj);
            // Remove item from outside pool
            outsidePool.Remove(obj);

            // Deactivate the attached gameobject
            obj.gameObject.SetActive(false);
        }

        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        public T Depool()
        {
            T obj;
            // If pool is empty consider creating a new object
            if (pool.Count == 0)
            {
                // If pool is able to expand, clone and return the model object
                if (type == PoolType.EXPANDABLE)
                {
                    GameObject gameObj = UnityEngine.Object.Instantiate(modelObj.gameObject);
                    gameObj.name = nameT + " " + numCreated;
                    numCreated++;

                    obj = gameObj.GetComponent<T>();

                    // Initialise the object manually as the start method won't run this frame
                    obj.Reset();
                }
                else
                    throw new Exception("Pool is empty and is not set to be expandable.");
            }
            else
            {
                // Random index
                int itemIndex = type == PoolType.RANDOM ? UnityEngine.Random.Range(0, pool.Count) : 0;
                // Take object out of pool and return it
                obj = pool[itemIndex];
                // Remove item from the pool so it can't be retaken
                pool.RemoveAt(itemIndex);

                // Take out of parent object
                obj.transform.SetParent(null);
                // Call the IResettable reset method
                obj.Reset();
            }

            // Activate object
            obj.gameObject.SetActive(true);

            // Add to outside pool
            outsidePool.Add(obj);

            return obj;
        }

        // Repool all objects
        public void RetrieveAll()
        {
            // Check for and remove duplicate references
            outsidePool = outsidePool.Distinct().ToList();

            foreach (T obj in outsidePool)
                Enpool(obj);
        }
    }
}