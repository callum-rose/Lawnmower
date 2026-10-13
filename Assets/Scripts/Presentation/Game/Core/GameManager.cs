using System.Collections;
using Core;
using Exceptions;
using Game.Levels;
using Game.Mowers;
using Game.Input;
using Game.UI.Screens;
using Game.UndoSystem;
using Core.EventChannels;
using Lawnmower.Model;
using Lawnmower.Presenters;
using Lawnmower.Services;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Core
{
	/// <summary>
	/// Composition root for the Game scene: wires a <see cref="GamePresenter"/>, the HUD and the tutorial to this
	/// scene's views for each level, and opens input once the level intro has played.
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
		[SerializeField] private GameScreenView screen;
		[SerializeField] private Camera gameCamera;

		[Header("Event Channels")]
		[SerializeField] private IVoidEventChannelTransmitterContainer startPlayingEventChannel;
		[SerializeField] private IVoidEventChannelTransmitterContainer stopPlayingEventChannel;

		private IVoidEventChannelTransmitter StartPlayingEventChannel => startPlayingEventChannel.Result;
		private IVoidEventChannelTransmitter StopPlayingEventChannel => stopPlayingEventChannel.Result;

		private GamePresenter _presenter;
		private GameHudPresenter _hud;
		private GameFeedbackPresenter _feedback;
		private TutorialPresenter _tutorial;
		private bool _showTutorial;
		private Coroutine _startPlayingTimer;

		/// <summary>The current session's presenter, or null between levels.</summary>
		public GamePresenter Presenter => _presenter;

		/// <summary>The running simulation, or null between levels.</summary>
		public LevelSimulation Simulation => _presenter?.Simulation;

		internal GameScreenView Screen => screen;

		internal GameHudPresenter Hud => _hud;

		/// <summary>The tutorial, while it runs.</summary>
		internal TutorialPresenter Tutorial => _tutorial;

		internal MoveInputController MoveInput => moveInput;

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

			GameNavigator navigator = new GameNavigator(levelDataManager, input);
			_presenter = new GamePresenter(
				new GameSessionSetup(level.ToDefinition(), level.Par),
				levelManager,
				mowerManager,
				screen,
				navigator,
				GameServices.Progression,
				new UnityClock());

			_hud = new GameHudPresenter(_presenter, screen, GameServices.Progression, navigator);
			_hud.SettingsRequested += OpenSettings;
			undoController.UndoRequested += _presenter.Undo;
			undoController.RedoRequested += _presenter.Redo;
			screen.SetFailAnchor(mowerManager.GetMowerWorldPosition, gameCamera);

			_presenter.AttachInput(moveInput.Input);
			_presenter.Start();
			_hud.Start();
			_feedback = GameServices.CreateFeedback(_presenter.Simulation);

			if (input.IsTutorial)
			{
				TutorialLauncher.Request();
			}

			_showTutorial = TutorialLauncher.ShouldShow(
				GameServices.Progression.IndexOf(level.Id) == 0,
				PersistantData.LevelModule.TutorialCompleted.Load());

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

			_tutorial?.Dispose();
			_tutorial = null;
			_feedback?.Dispose();
			_feedback = null;

			if (_hud != null)
			{
				_hud.SettingsRequested -= OpenSettings;
				_hud.Dispose();
				_hud = null;
			}

			if (_presenter != null)
			{
				undoController.UndoRequested -= _presenter.Undo;
				undoController.RedoRequested -= _presenter.Redo;
				_presenter.DetachInput(moveInput.Input);
				_presenter.Dispose();
				_presenter = null;
			}

			if (clearViews)
			{
				levelManager.Clear();
				mowerManager.Hide();
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

			if (_showTutorial)
			{
				_tutorial = new TutorialPresenter(screen.Tutorial, moveInput.Input,
					() => PersistantData.LevelModule.TutorialCompleted.Save(true));
				_tutorial.Finished += () => _tutorial = null;
				_tutorial.Start();
			}
		}

		/// <summary>Pause → SETTINGS. A placeholder until Final integration sets <see cref="PauseSettingsHook.Open"/>.</summary>
		private void OpenSettings()
		{
			if (PauseSettingsHook.Open != null)
			{
				PauseSettingsHook.Open(null);
				return;
			}

			Debug.Log("[Game] Pause > Settings requested: not wired yet (Final integration sets PauseSettingsHook.Open to G4's Settings prefab).");
			screen.ShowToast("Settings coming soon");
		}

		#endregion
	}
}
