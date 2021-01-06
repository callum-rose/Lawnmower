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
		private GameObject _modelObj;

		// Number of objects the pool has instantiated
		private int _numCreated;

		// Parent transform to contain all pooled items
		private Transform _parentContainer;

		// Name of objects in pool
		private string _nameT;

		/// <summary>
		/// Gets the number of objects in the pool
		/// </summary>
		public int Size => ItemsInPool.Count;

		/// <summary>
		/// Initialises a new GameObjectPool reference
		/// </summary>
		public GameObjectPool(string name, GameObject modelObj)
		{
			//save reference to gameobject
			_modelObj = modelObj;

			Init(name);

			Put(modelObj);
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
				obj.hideFlags = HideFlags.HideAndDontSave;

				if (_parentContainer)
				{
					obj.transform.SetParent(_parentContainer);
				}

				// Put item in pool
				ItemsInPool.Add(obj);
				// Remove item from outside pool
				ItemsOutsidePool.Remove(obj);
			}
			else
			{
				Debug.LogWarning(string.Format(
					"GameObject {0} put into {1} Pool is a prefab. I've instantiated it and put it in the pool :)",
					obj.name, _nameT));

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
				gameObj = UnityEngine.Object.Instantiate(_modelObj.gameObject);
				gameObj.name = _nameT + " " + _numCreated;
				_numCreated++;
			}
			else
			{
				// Random index or zeroth
				int itemIndex = 0;
				// Take object out of pool and return it
				gameObj = ItemsInPool[itemIndex];
				// Remove item from the pool so it can't be retaken
				ItemsInPool.RemoveAt(itemIndex);

				// Take out of parent object
				gameObj.transform.SetParent(null);
			}

			// Activate object
			gameObj.SetActive(true);
			gameObj.hideFlags = HideFlags.None;

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
				Debug.LogWarning((ItemsOutsidePool.Count - tempOutsidePool.Length) +
				                 " duplicate reference(s) in outsidePool for some reason.");

			for (int i = tempOutsidePool.Length - 1; i >= count; i--)
				Put(tempOutsidePool[i]);
		}

		private void Init(string name)
		{
			ItemsInPool = new List<GameObject>();
			ItemsOutsidePool = new List<GameObject>();

			// create parent gameobject to hold pooled items
			_parentContainer = new GameObject().transform;
			_parentContainer.name = name + " Pool";

			_nameT = name;
		}
	}
}