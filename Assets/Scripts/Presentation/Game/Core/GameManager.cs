using System.Collections;
using Core;
using Exceptions;
using Game.Levels;
using Game.Mowers;
using Game.Input;
using Game.UndoSystem;
using Core.EventChannels;
using Lawnmower.Model;
using Lawnmower.Presenters;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Core
{
	/// <summary>
	/// Composition root for the Game scene: wires a <see cref="GamePresenter"/> to this scene's views
	/// for each level, and opens its input once the level intro has played.
	/// </summary>
	internal class GameManager : BaseSceneManager
	{
		[FormerlySerializedAs("mowerObjectCreator"), FormerlySerializedAs("mowerCreator"), SerializeField]
		private MowerManager mowerManager;

		[SerializeField] private LevelManager levelManager;
		[SerializeField] private LevelDataManager levelDataManager;
		[SerializeField] private UndoController undoController;
		[SerializeField] private GamePlayData gamePlayData;
		[SerializeField] private MoveInputController moveInput;

		[Header("Event Channels")]
		[SerializeField] private OpenDialogEventChannel openDialogEventChannel;
		[SerializeField] private CloseDialogEventChannel closeDialogEventChannel;
		[SerializeField] private IVoidEventChannelTransmitterContainer startPlayingEventChannel;
		[SerializeField] private IVoidEventChannelTransmitterContainer stopPlayingEventChannel;
		[SerializeField] private UndoableEventChannel levelFailedChannel;

		private IVoidEventChannelTransmitter StartPlayingEventChannel => startPlayingEventChannel.Result;
		private IVoidEventChannelTransmitter StopPlayingEventChannel => stopPlayingEventChannel.Result;

		private GamePresenter _presenter;
		private DialogGameScreenView _screen;
		private Coroutine _startPlayingTimer;

		/// <summary>The current session's presenter, or null between levels.</summary>
		public GamePresenter Presenter => _presenter;

		/// <summary>The running simulation, or null between levels.</summary>
		public LevelSimulation Simulation => _presenter?.Simulation;

		internal DialogGameScreenView Screen => _screen;

		internal MoveInputController MoveInput => moveInput;

		/// <summary>Play-through tests set this so they don't write the player's saved progress.</summary>
		internal static ILevelProgress ProgressOverrideForTests { get; set; }

		#region Unity

		private void OnDestroy()
		{
			// The scene is unloading and destroys the views itself.
			End(false);
		}

		#endregion

		#region API

		public override void Begin(object data)
		{
			End();

			if (data is not GameSetupPassThroughData input)
			{
				throw new InvalidTypeException(data, typeof(GameSetupPassThroughData));
			}

			LevelData level = input.Level;
			mowerManager.SetMower(input.Mower);

			_screen = new DialogGameScreenView(openDialogEventChannel, closeDialogEventChannel, levelFailedChannel, undoController);
			_presenter = new GamePresenter(
				new GameSessionSetup(level.ToDefinition(), level.Par),
				levelManager,
				mowerManager,
				_screen,
				new GameNavigator(levelDataManager, input),
				ProgressOverrideForTests ?? new LevelProgressStore(levelDataManager, level),
				new UnityClock());

			_presenter.AttachInput(moveInput.Input);
			_presenter.Start();

			_startPlayingTimer = StartCoroutine(IntroRoutine());
		}

		public void End() => End(true);

		private void End(bool clearViews)
		{
			if (_startPlayingTimer != null)
			{
				StopCoroutine(_startPlayingTimer);
				_startPlayingTimer = null;
			}

			_presenter?.DetachInput(moveInput.Input);
			_presenter?.Dispose();
			_presenter = null;

			if (clearViews)
			{
				levelManager.Clear();
				mowerManager.Hide();
			}

			_screen?.Dispose();
			_screen = null;

			if (clearViews)
			{
				StopPlayingEventChannel?.Raise();
			}
		}

		#endregion

		#region Events

		private IEnumerator IntroRoutine()
		{
			yield return new WaitForSeconds(gamePlayData.LevelIntroDuration);

			// The mower drops in once the tiles have animated in; moves before that would animate a hidden mower.
			yield return new WaitUntil(() => mowerManager.IsMowerVisible);

			OnIntroFinished();
		}

		private void OnIntroFinished()
		{
			_startPlayingTimer = null;
			_presenter.BeginPlay();
			StartPlayingEventChannel.Raise();
		}

		#endregion
	}
}
