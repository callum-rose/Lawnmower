using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Lawnmower.Editor.Capture
{
	/// <summary>
	/// Video counterpart to <see cref="GameCapture"/>, the same in a live Editor and in batch mode:
	/// <para>Live:  unity command record --scene Assets/Scenes/MainMenu.unity --duration 5</para>
	/// <para>Batch: unity run "$PWD" --command record -- --scene Assets/Scenes/MainMenu.unity --duration 5</para>
	/// Enters Play Mode (with domain reload turned off for the run, so this command survives it), fixes the game clock
	/// to the video frame rate with <see cref="Time.captureFramerate"/>, and renders the camera plus Screen Space -
	/// Overlay UI into each frame. The video plays at real speed however slowly the frames render. No audio.
	/// The Game view is sized to the video first (to its shape, if the display is too small), as the game lays out from
	/// Screen; in a live Editor that leaves the Game view on the video's resolution afterwards.
	/// </summary>
	public static class GameRecord
	{
		private const int MaxDimension = 4096;

		public sealed class Result
		{
			public string Path;
			public int Width;
			public int Height;
			public int Fps;
			public int Frames;
			public float Seconds;
			public string Scene;
			public string Camera;
			public string Screen;
			public int OverlayCanvases;
			public bool BatchMode;
			public string Setup;
		}

		[CliCommand("record",
			"Record a game camera to an MP4/WebM video, in a live Editor or batch mode. Opens --scene, enters Play Mode " +
			"(already in Play Mode: records the running game and stays in it), runs an optional static --setup method, " +
			"waits --delay seconds, then records --duration seconds of game time at --fps with Screen Space - Overlay UI " +
			"included. Game time is locked to the frame rate, so the video plays at real speed.",
			Tags = new[] { "capture" })]
		public static Task<Result> Record(
			[CliArg("scene", "Scene to open before entering Play Mode, e.g. Assets/Scenes/MainMenu.unity. Default: the open scene(s).")]
			string scene = "",
			[CliArg("camera", "Camera GameObject name (disabled cameras count). Default: Camera.main, else the first camera. " +
				"Looked up after --setup and --delay, so it can be a camera the game spawns.")]
			string camera = "",
			[CliArg("width", "Output width in pixels, rounded down to even (default 720, a portrait phone).")]
			int width = 720,
			[CliArg("height", "Output height in pixels, rounded down to even (default 1558).")]
			int height = 1558,
			[CliArg("fps", "Frames per second (default 30).")]
			int fps = 30,
			[CliArg("duration", "Seconds of game time to record (default 5).")]
			float duration = 5f,
			[CliArg("delay", "Seconds of game time to let run after --setup before recording starts (default 0.5).")]
			float delay = 0.5f,
			[CliArg("ui", "Include Screen Space - Overlay canvases (default true).")]
			bool ui = true,
			[CliArg("setup", "Static method to run once Play Mode has started, as Namespace.Type.Method, e.g. " +
				"Lawnmower.Editor.GameScreens.GameScreensDebug.LoadLevel. It may take no parameters or one string, number, " +
				"bool or enum (--setup_arg); its return value is reported.")]
			string setup = "",
			[CliArg("setup_arg", "Argument passed to the --setup method when it takes one.")]
			string setupArg = "",
			[CliArg("output", "Output file, absolute or relative to the project; .mp4 (H.264) or .webm (VP8). " +
				"Default: Captures/<scene>-<camera>-<time>.mp4. Not Temp/: Unity deletes it on exit")]
			string output = "")
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				throw new InvalidOperationException(
					"No graphics device: the Editor was started with -nographics, so nothing can render. Run without it.");
			}

			if (EditorApplication.isPlaying && !string.IsNullOrEmpty(scene))
			{
				throw new InvalidOperationException("--scene can't be used in Play Mode; it records the running game.");
			}

			if (EditorApplication.isCompiling || EditorApplication.isUpdating)
			{
				throw new InvalidOperationException("The Editor is compiling or importing; try again once it's idle.");
			}

			string extension = string.IsNullOrEmpty(output) ? ".mp4" : Path.GetExtension(output).ToLowerInvariant();
			if (extension != ".mp4" && extension != ".webm")
			{
				throw new ArgumentException($"--output must end in .mp4 or .webm, not '{extension}'.");
			}

			var session = new Session
			{
				Width = Mathf.Clamp(width, 2, MaxDimension) & ~1,
				Height = Mathf.Clamp(height, 2, MaxDimension) & ~1,
				Fps = Mathf.Clamp(fps, 1, 120),
				CameraName = camera,
				Ui = ui,
				Setup = setup,
				SetupArg = setupArg,
				Output = output,
				Extension = extension,
			};
			session.DelayFrames = Mathf.Max(0, Mathf.RoundToInt(delay * session.Fps));
			session.TotalFrames = Mathf.Max(1, Mathf.RoundToInt(duration * session.Fps));
			session.Start(scene);
			return session.Completion.Task;
		}

		/// <summary>One recording, driven a frame at a time from <see cref="EditorApplication.update"/>.</summary>
		private sealed class Session
		{
			private enum Step { EnteringPlayMode, MatchScreen, Setup, Delay, Recording, ExitingPlayMode }

			public readonly TaskCompletionSource<Result> Completion = new TaskCompletionSource<Result>();

			public int Width, Height, Fps, DelayFrames, TotalFrames;
			public string CameraName, Setup, SetupArg, Output, Extension;
			public bool Ui;

			private Step _step;
			private bool _enteredPlayMode;
			private bool _previousOptionsEnabled;
			private bool _settingsWereDirty;
			private EnterPlayModeOptions _previousOptions;
			private SceneSetup[] _restoreScenes;
			private int _lastFrame = -1;
			private int _waitedFrames;
			private string _screen;
			private Vector2Int _screenTarget, _lastScreen;
			private string _setupResult;

			private Camera _cam;
			private RenderTexture _rt;
			private Texture2D _frame;
			private RenderTexture _previousTarget;
			private int _previousMask;
			private readonly List<(Canvas canvas, float planeDistance)> _moved = new List<(Canvas, float)>();
			private readonly HashSet<Canvas> _overlays = new HashSet<Canvas>();
			private MediaEncoder _encoder;
			private string _path;
			private int _recorded;
			private Result _result;
			private Exception _error;

			public void Start(string scene)
			{
				if (!string.IsNullOrEmpty(scene))
				{
					_restoreScenes = GameCapture.OpenScene(scene);
				}

				// The game lays itself out from Screen (the board framer does), so it has to be the video's size.
				// Batch mode's hidden Game view ignores this and is resized instead, in MatchScreen.
				PlayModeWindow.SetCustomRenderingResolution((uint)Width, (uint)Height, $"Record {Width}x{Height}");

				EditorApplication.update += Tick;
				if (EditorApplication.isPlaying)
				{
					_step = Step.MatchScreen;
					return;
				}

				// A domain reload would tear down this command (and the Pipeline server waiting on it) mid-run.
				_settingsWereDirty = EditorUtility.IsDirty(EditorSettingsAsset);
				_previousOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
				_previousOptions = EditorSettings.enterPlayModeOptions;
				EditorSettings.enterPlayModeOptionsEnabled = true;
				EditorSettings.enterPlayModeOptions = _previousOptionsEnabled
					? _previousOptions | EnterPlayModeOptions.DisableDomainReload
					: EnterPlayModeOptions.DisableDomainReload;

				_enteredPlayMode = true;
				_step = Step.EnteringPlayMode;
				EditorApplication.EnterPlaymode();
			}

			private static Object EditorSettingsAsset => Unsupported.GetSerializedAssetInterfaceSingleton("EditorSettings");

			private void Tick()
			{
				try
				{
					switch (_step)
					{
						case Step.EnteringPlayMode:
							if (EditorApplication.isPlaying)
							{
								_step = Step.MatchScreen;
							}
							else if (!EditorApplication.isPlayingOrWillChangePlaymode)
							{
								throw new InvalidOperationException(
									"Play Mode didn't start; check the Console for compile errors or exceptions on enter.");
							}

							break;

						case Step.MatchScreen:
							if (!NextFrame())
							{
								break;
							}

							var screen = new Vector2Int(Screen.width, Screen.height);
							if (_screenTarget == Vector2Int.zero)
							{
								_screenTarget = new Vector2Int(Width, Height);
							}
							else if (screen == _lastScreen && screen != _screenTarget)
							{
								// The window stopped growing (it can't be taller than the display): match the aspect instead,
								// which is what framing depends on.
								float fit = Mathf.Min((float)screen.x / _screenTarget.x, (float)screen.y / _screenTarget.y, 1f);
								_screenTarget = Vector2Int.RoundToInt((Vector2)_screenTarget * fit);
							}

							_lastScreen = screen;
							bool matched = Mathf.Abs(screen.x - _screenTarget.x) <= 1 && Mathf.Abs(screen.y - _screenTarget.y) <= 1;
							if (matched || ++_waitedFrames > 10 || !Application.isBatchMode)
							{
								_screen = $"{screen.x}x{screen.y}";
								if (Mathf.Abs((float)screen.x / screen.y - (float)Width / Height) > 0.005f)
								{
									Debug.LogWarning($"[GameRecord] Screen is {_screen}, not the shape of {Width}x{Height}; " +
										"anything the game lays out from Screen may be framed for that shape instead.");
								}

								_waitedFrames = 0;
								_step = Step.Setup;
							}
							else
							{
								ResizeGameViews(_screenTarget);
							}

							break;

						case Step.Setup:
							if (!NextFrame())
							{
								break;
							}

							_setupResult = string.IsNullOrEmpty(Setup) ? null : GameCapture.RunSetup(Setup, SetupArg);
							Time.captureFramerate = Fps;
							_waitedFrames = 0;
							_step = Step.Delay;
							break;

						case Step.Delay:
							if (!NextFrame())
							{
								break;
							}

							if (++_waitedFrames >= DelayFrames)
							{
								BeginRecording();
								_step = Step.Recording;
							}

							break;

						case Step.Recording:
							if (!NextFrame())
							{
								break;
							}

							RecordFrame();
							if (_recorded >= TotalFrames)
							{
								_result = FinishRecording();
								Finish();
							}

							break;

						case Step.ExitingPlayMode:
							if (!EditorApplication.isPlayingOrWillChangePlaymode)
							{
								Complete();
							}

							break;
					}
				}
				catch (Exception e)
				{
					Fail(e);
				}
			}

			/// <summary>Batch mode's Game view is a hidden window whose size, not its chosen resolution, sets Screen. The
			/// size is corrected by what Screen reads a frame later, as the window's chrome isn't counted the same.</summary>
			private void ResizeGameViews(Vector2Int target)
			{
				Type playModeView = typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeView");
				float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
				foreach (Object view in Resources.FindObjectsOfTypeAll(playModeView))
				{
					var window = (EditorWindow)view;
					Rect position = window.position;
					position.width += (target.x - Screen.width) / pixelsPerPoint;
					position.height += (target.y - Screen.height) / pixelsPerPoint;
					window.position = position;
				}
			}

			/// <summary>True once per game frame: the Editor can tick more often than the player loop.</summary>
			private bool NextFrame()
			{
				if (!EditorApplication.isPlaying)
				{
					throw new InvalidOperationException("Play Mode stopped before the recording finished.");
				}

				if (Time.frameCount == _lastFrame)
				{
					return false;
				}

				_lastFrame = Time.frameCount;
				return true;
			}

			private void BeginRecording()
			{
				_cam = GameCapture.FindCamera(CameraName);
				_path = GameCapture.ResolveOutput(Output, _cam, Extension);
				Directory.CreateDirectory(Path.GetDirectoryName(_path));

				_rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
				_frame = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
				_previousTarget = _cam.targetTexture;
				_previousMask = _cam.cullingMask;

				var attributes = new VideoTrackAttributes
				{
					frameRate = new MediaRational(Fps),
					width = (uint)Width,
					height = (uint)Height,
					includeAlpha = false,
					bitRateMode = VideoBitrateMode.High,
				};
				_encoder = new MediaEncoder(_path, attributes);

				// Kept on the texture for the whole run: switching per frame between it and the screen, at different
				// sizes, corrupts the camera's temporal history (TAA, motion vectors) and the picture breaks up.
				_cam.targetTexture = _rt;
				_cam.ResetAspect();
			}

			private void RecordFrame()
			{
				if (_cam == null)
				{
					throw new InvalidOperationException("The recording camera was destroyed mid-recording.");
				}

				// Overlay canvases go onto the camera for this render only: game code reads their layout as screen
				// pixels (the board framer does), which stops being true while they're in camera space.
				if (Ui)
				{
					_moved.AddRange(GameCapture.MoveOverlaysOnto(_cam));
					foreach ((Canvas canvas, float _) in _moved)
					{
						_overlays.Add(canvas);
					}
				}

				RenderTexture previousActive = RenderTexture.active;
				try
				{
					Canvas.ForceUpdateCanvases();
					_cam.Render();
					RenderTexture.active = _rt;
					_frame.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
					_frame.Apply(false);
				}
				finally
				{
					RenderTexture.active = previousActive;
					RestoreOverlays();
				}

				_encoder.AddFrame(_frame);
				_recorded++;
			}

			private Result FinishRecording()
			{
				var result = new Result
				{
					Path = _path,
					Width = Width,
					Height = Height,
					Fps = Fps,
					Frames = _recorded,
					Seconds = (float)_recorded / Fps,
					Scene = string.Join(",", GameCapture.OpenScenePaths()),
					Camera = _cam != null ? _cam.name : CameraName,
					Screen = _screen,
					OverlayCanvases = _overlays.Count,
					BatchMode = Application.isBatchMode,
					Setup = _setupResult,
				};
				ReleaseRecording();
				Debug.Log($"[GameRecord] wrote {_path} ({Width}x{Height}, {_recorded} frames at {Fps} fps, camera " +
					$"{result.Camera}, {result.OverlayCanvases} overlay canvases)");
				return result;
			}

			private void RestoreOverlays()
			{
				foreach ((Canvas canvas, float planeDistance) in _moved)
				{
					if (canvas != null)
					{
						canvas.renderMode = RenderMode.ScreenSpaceOverlay;
						canvas.worldCamera = null;
						canvas.planeDistance = planeDistance;
					}
				}

				if (_moved.Count > 0)
				{
					_moved.Clear();
					Canvas.ForceUpdateCanvases();
				}

				if (_cam != null)
				{
					_cam.cullingMask = _previousMask;
				}
			}

			private void ReleaseRecording()
			{
				_encoder?.Dispose();
				_encoder = null;

				RestoreOverlays();
				if (_cam != null)
				{
					_cam.targetTexture = _previousTarget;
					_cam.ResetAspect();
				}

				if (_rt != null)
				{
					_rt.Release();
					Object.DestroyImmediate(_rt);
					_rt = null;
				}

				if (_frame != null)
				{
					Object.DestroyImmediate(_frame);
					_frame = null;
				}
			}

			/// <summary>Leaves Play Mode if this run entered it, then completes from <see cref="Complete"/>.</summary>
			private void Finish()
			{
				if (Time.captureFramerate == Fps)
				{
					Time.captureFramerate = 0;
				}

				if (_enteredPlayMode && EditorApplication.isPlayingOrWillChangePlaymode)
				{
					_step = Step.ExitingPlayMode;
					EditorApplication.ExitPlaymode();
					return;
				}

				Complete();
			}

			private void Complete()
			{
				EditorApplication.update -= Tick;
				if (_enteredPlayMode)
				{
					EditorSettings.enterPlayModeOptionsEnabled = _previousOptionsEnabled;
					EditorSettings.enterPlayModeOptions = _previousOptions;

					// Otherwise Unity rewrites ProjectSettings/EditorSettings.asset on quit, though nothing changed.
					if (!_settingsWereDirty && EditorSettingsAsset != null)
					{
						EditorUtility.ClearDirty(EditorSettingsAsset);
					}
				}

				if (_restoreScenes != null && _restoreScenes.Length > 0 && !Application.isBatchMode)
				{
					EditorSceneManager.RestoreSceneManagerSetup(_restoreScenes);
				}

				if (_error != null)
				{
					Completion.TrySetException(_error);
				}
				else
				{
					Completion.TrySetResult(_result);
				}
			}

			private void Fail(Exception e)
			{
				Debug.LogException(e);
				ReleaseRecording();
				if (_path != null && File.Exists(_path))
				{
					File.Delete(_path);
				}

				_error ??= e;

				try
				{
					Finish();
				}
				catch (Exception cleanup)
				{
					Debug.LogException(cleanup);
					EditorApplication.update -= Tick;
					Completion.TrySetException(_error);
				}
			}
		}
	}
}
