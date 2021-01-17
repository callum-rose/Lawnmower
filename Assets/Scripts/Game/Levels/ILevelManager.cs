using System;
using Game.Core;
using Game.Levels.Editorr;
using Game.UndoSystem;

namespace Game.Levels
{
	internal interface ILevelManager : IHasEditMode
	{
		event UndoableAction LevelCompleted;
		event UndoableAction LevelFailed;

		IReadOnlyLevelData Level { get; }
		event Action LevelChanged;

		GridVector MowerPosition { get; }

		void Init(IReadOnlyLevelData level);
	}
}