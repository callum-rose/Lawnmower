using System;
using Core;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Core
{
	internal class LevelDataRecorder : IDisposable
	{
		private readonly IUndoSystem _undoSystem;

		public float Duration => _startTime != null && _endTime != null ? _endTime.Value - _startTime.Value : 0;
		public int? UndoCount { get; private set; }

		private float? _startTime, _endTime;

		#region API

		public LevelDataRecorder(IUndoSystem undoSystem)
		{
			_undoSystem = undoSystem;
			_undoSystem.Undone += UndoSystemOnUndone;
			_undoSystem.Redone += UndoSystemOnRedone;
		}

		public void StartRecording()
		{
			UndoCount = 0;
			_startTime = Time.time;
			_endTime = null;
		}

		public void StopRecording()
		{
			_endTime = Time.time;
		}

		public PersistantData.LevelModule.MetaData ExtractData()
		{
			return new PersistantData.LevelModule.MetaData
			{
				timeTaken = Duration,
				undoCount = UndoCount ?? 0
			};
		}

		public void Dispose()
		{
			_undoSystem.Undone -= UndoSystemOnUndone;
			_undoSystem.Redone -= UndoSystemOnRedone;
		}

		#endregion

		#region Events

		private void UndoSystemOnRedone()
		{
			UndoCount--;
		}

		private void UndoSystemOnUndone()
		{
			UndoCount++;
		}

		#endregion
	}
}