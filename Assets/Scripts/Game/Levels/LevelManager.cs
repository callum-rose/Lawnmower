using System;
using System.Linq;
using Game.Core;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
	internal class LevelManager : MonoBehaviour, ILevelManager
	{
		[TitleGroup("Scene")] [SerializeField, Required]
		private LevelObjectFactory levelFactory;

		[TitleGroup("Assets"), SerializeField] 
		private HeadlessLevelManager headlessLevelManager;

		public bool IsEditMode
		{
			get => headlessLevelManager.IsEditMode;
			set => headlessLevelManager.IsEditMode = value;
		}
		
		public event UndoableAction LevelCompleted
		{
			add => headlessLevelManager.LevelCompleted += value;
			remove => headlessLevelManager.LevelCompleted -= value;
		}
		public event UndoableAction LevelFailed
		{
			add => headlessLevelManager.LevelFailed += value;
			remove => headlessLevelManager.LevelFailed -= value;
		}

		public IReadOnlyLevelData Level => headlessLevelManager.Level;
		public event Action LevelChanged
		{
			add => headlessLevelManager.LevelChanged += value;
			remove => headlessLevelManager.LevelChanged -= value;
		}

		public GridVector MowerPosition => headlessLevelManager.MowerPosition;

		private GameObject[,] _tileObjects;

		public void Init(IReadOnlyLevelData level)
		{
			Clear();
			
			headlessLevelManager.Init(level);
			
			_tileObjects = levelFactory.Build(level);
		}
		
		public void Clear()
		{
			if (headlessLevelManager)
			{
				headlessLevelManager.Clear();
			}

			if (_tileObjects == null)
			{
				return;
			}

			levelFactory.Remove(_tileObjects.Cast<GameObject>().Where(t => t != null));
		}
	}
}