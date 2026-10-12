using System;
using System.Linq;
using Lawnmower.Model;
using UnityEngine;

namespace Game.Levels
{
	/// <summary>Builds and clears the tile views for the current level.</summary>
	internal class LevelManager : MonoBehaviour
	{
		[Header("Scene")] [SerializeField]
		private LevelObjectFactory levelFactory;

		[Header("Event Channels")] [SerializeField]
		private LevelDataEventChannel levelStartedEventChannel;

		public Level Level => _level;

		private Level _level;
		private GameObject[,] _tileObjects;

		public void Init(Level level)
		{
			_level = level ?? throw new ArgumentNullException(nameof(level));

			if (levelStartedEventChannel)
			{
				levelStartedEventChannel.Raise(level);
			}
		}

		/// <summary>Called by a Timeline signal in Game.unity; keep the name.</summary>
		public void Build()
		{
			if (_level == null)
			{
				throw new NullReferenceException("Level is null");
			}
			
			_tileObjects = levelFactory.Build(_level);
		}
		
		public void Clear()
		{
			_level = null;

			if (_tileObjects == null)
			{
				return;
			}

			levelFactory.Remove(_tileObjects.Cast<GameObject>().Where(t => t != null));
			_tileObjects = null;
		}
	}
}
