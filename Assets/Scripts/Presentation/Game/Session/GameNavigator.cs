using Core;
using Game.Levels;
using Lawnmower.Presenters;

namespace Game.Core
{
	internal sealed class GameNavigator : IGameNavigator
	{
		private readonly LevelDataManager _levels;
		private readonly GameSetupPassThroughData _current;

		public GameNavigator(LevelDataManager levels, GameSetupPassThroughData current)
		{
			_levels = levels;
			_current = current;
		}

		public bool HasNextLevel => _levels.GetLevelAfter(_current.Level, out _);

		public void LoadNextLevel()
		{
			if (_levels.GetLevelAfter(_current.Level, out LevelInfo next))
			{
				ViewManager.Instance.Load(UnityScene.Game, new GameSetupPassThroughData(_current.Mower, next.LevelData));
			}
			else
			{
				LoadLevelSelect();
			}
		}

		public void RestartLevel()
		{
			ViewManager.Instance.Load(UnityScene.Game, _current);
		}

		public void LoadLevelSelect()
		{
			ViewManager.Instance.Load(UnityScene.LevelSelect);
		}
	}
}
