using System;
using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using UnityEngine;

namespace Game.Tutorial
{
	internal class AppSetupManager : MonoBehaviour
	{
		[SerializeField] private LevelDataManager levelDataManager;
		[SerializeField] private MowerDataManager mowerDataManager;
		
		private void Awake()
		{
			Application.targetFrameRate = Screen.currentResolution.refreshRate;
			
			bool isTutorialIncomplete = !PersistantData.LevelModule.TutorialCompleted.Load();
			
			if (isTutorialIncomplete)
			{
				Guid mowerId = PersistantData.MowerModule.CurrentId.Load();
				MowerData mowerData = mowerDataManager.GetMowerData(mowerId);

				LevelInfo levelInfo = levelDataManager.GetLevel(0);
				IReadOnlyLevelData levelData = levelInfo.LevelData;
				
				ViewManager.Instance.Load(UnityScene.Game, new GameSetupPassThroughData(mowerData, levelData, true));
			}
			else
			{
				//ViewManager.Instance.Load(UnityScene.MainMenu);
			}
		}
	}
}
