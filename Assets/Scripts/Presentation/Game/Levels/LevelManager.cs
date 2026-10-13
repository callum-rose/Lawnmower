using System;
using System.Linq;
using Lawnmower.Model;
using Lawnmower.Presenters;
using UnityEngine;

namespace Game.Levels
{
	/// <summary>The board view: builds and clears the tile views for the current level.</summary>
	internal class LevelManager : MonoBehaviour, IBoardView
	{
		[Header("Scene")] [SerializeField]
		private LevelObjectFactory levelFactory;

		[Header("Event Channels")] [SerializeField]
		private LevelDataEventChannel levelStartedEventChannel;

		public Level Level => _level;

		private Level _level;
		private GameObject[,] _tileObjects;

		/// <summary>The tiles are built later, by <see cref="Build"/> from the intro timeline.</summary>
		public void Show(Level level)
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
