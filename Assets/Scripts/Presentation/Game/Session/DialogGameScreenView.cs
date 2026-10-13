using System;
using Core.EventChannels;
using Game.UndoSystem;
using Lawnmower.Presenters;
using UI.Buttons;
using UI.Dialogs;

namespace Game.Core
{
	/// <summary>
	/// The game screen on today's dialog system. G3 replaces it with the Figma HUD and the Level
	/// Complete / Level Failed cards.
	/// </summary>
	internal sealed class DialogGameScreenView : IGameScreenView, IDisposable
	{
		public event Action UndoRequested;
		public event Action RedoRequested;
		public event Action RestartRequested;
		public event Action NextLevelRequested;

		/// <summary>The HUD quit button (UIManager) still loads Level Select itself until G3.</summary>
		public event Action LevelSelectRequested
		{
			add { }
			remove { }
		}

		private readonly OpenDialogEventChannel _openDialog;
		private readonly CloseDialogEventChannel _closeDialog;
		private readonly UndoableEventChannel _levelFailedChannel;
		private readonly UndoController _undoController;

		private int _completedDialogId;
		private int _failedDialogId;

		public int Moves { get; private set; }
		public int Par { get; private set; }

		public DialogGameScreenView(OpenDialogEventChannel openDialog, CloseDialogEventChannel closeDialog,
			UndoableEventChannel levelFailedChannel, UndoController undoController)
		{
			_openDialog = openDialog;
			_closeDialog = closeDialog;
			_levelFailedChannel = levelFailedChannel;
			_undoController = undoController;

			if (_undoController)
			{
				_undoController.UndoRequested += OnUndo;
				_undoController.RedoRequested += OnRedo;
				_undoController.RestartRequested += OnRestart;
			}
		}

		public void Dispose()
		{
			if (_undoController)
			{
				_undoController.UndoRequested -= OnUndo;
				_undoController.RedoRequested -= OnRedo;
				_undoController.RestartRequested -= OnRestart;
			}
		}

		#region IGameScreenView

		public void ShowProgress(int moves, int par)
		{
			// No HUD counter yet (G3); kept for tests and tools.
			Moves = moves;
			Par = par;
		}

		public void ShowCompleted(LevelCompletedInfo info)
		{
			string body = info.Par > 0
				? $"{info.Moves} moves (par {info.Par}). {(info.IsPerfect ? "Lawn perfect!" : "Lawn done!")}"
				: $"{info.Moves} moves. Nice one";

			ButtonInfo next = new ButtonInfo(info.HasNextLevel ? "Next Level" : "Levels", action: () =>
			{
				_completedDialogId = 0;
				NextLevelRequested?.Invoke();
			});

			_completedDialogId = _openDialog.Raise(new DialogInfo("Level Completed!", body, next));
		}

		public void HideCompleted()
		{
			CloseDialog(ref _completedDialogId);
		}

		public void ShowFailed()
		{
			if (_levelFailedChannel)
			{
				_levelFailedChannel.Raise(false);
			}

			ButtonInfo undo = new ButtonInfo("Undo Last Move", action: () =>
			{
				_failedDialogId = 0;
				UndoRequested?.Invoke();
			});

			ButtonInfo restart = new ButtonInfo("Restart", action: () =>
			{
				_failedDialogId = 0;
				RestartRequested?.Invoke();
			});

			_failedDialogId = _openDialog.Raise(new DialogInfo("Scalped!", "You mowed down to the soil.", undo, restart));
		}

		public void HideFailed()
		{
			if (_levelFailedChannel)
			{
				_levelFailedChannel.Raise(true);
			}

			CloseDialog(ref _failedDialogId);
		}

		#endregion

		/// <summary>What the Level Failed dialog's "Undo Last Move" does; the undo then closes the dialog (play-through tests).</summary>
		internal void PressUndoLastMove()
		{
			UndoRequested?.Invoke();
		}

		internal bool IsFailedShown => _failedDialogId != 0;
		internal bool IsCompletedShown => _completedDialogId != 0;

		private void OnUndo() => UndoRequested?.Invoke();
		private void OnRedo() => RedoRequested?.Invoke();
		private void OnRestart() => RestartRequested?.Invoke();

		private void CloseDialog(ref int id)
		{
			if (id == 0)
			{
				return;
			}

			_closeDialog.Raise(id);
			id = 0;
		}
	}
}
