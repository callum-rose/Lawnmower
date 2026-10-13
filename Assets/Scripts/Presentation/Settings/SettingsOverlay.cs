using System;
using Lawnmower.Presenters;
using Lawnmower.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace Lawnmower.Screens
{
	/// <summary>
	/// The Settings overlay, openable from any scene: <c>SettingsOverlay.Show()</c>. It draws on its own canvas above
	/// everything else, binds to <see cref="GameServices.Settings"/> and <see cref="GameServices.Progression"/>, and
	/// destroys itself when closed (Back), returning to whatever opened it.
	/// </summary>
	public static class SettingsOverlay
	{
		/// <summary>The prefab under a Resources folder (Assets/Prefabs/UI/Settings/Resources/...).</summary>
		public const string ResourcePath = "BunnyHeaven/SettingsOverlay";

		/// <summary>
		/// Settings > "Replay tutorial" calls this after closing Settings. The tutorial owner (Game scene / Main Menu)
		/// sets it; while it's null the row only shows a "coming soon" toast and logs a warning.
		/// </summary>
		public static Action ReplayTutorialHook { get; set; }

		/// <summary>The open overlay, or null.</summary>
		public static SettingsOverlayView Current { get; private set; }

		public static bool IsOpen => Current != null;

		/// <summary>Raised after the overlay has closed.</summary>
		public static event Action Closed;

		/// <summary>Opens Settings over the current scene (or returns the one already open).</summary>
		/// <param name="onClosed">Called once when this overlay closes, e.g. to re-show the Pause card.</param>
		public static SettingsOverlayView Show(Action onClosed = null)
		{
			if (Current != null)
			{
				Current.AddClosedCallback(onClosed);
				return Current;
			}

			SettingsOverlayView prefab = Resources.Load<SettingsOverlayView>(ResourcePath);

			if (prefab == null)
			{
				Debug.LogError($"No SettingsOverlayView prefab at Resources/{ResourcePath}");
				return null;
			}

			EnsureEventSystem();
			SettingsOverlayView view = Object.Instantiate(prefab);
			view.name = prefab.name;
			Current = view;
			view.Open(GameServices.Settings, GameServices.Progression, Application.version, onClosed);
			return view;
		}

		/// <summary>Closes the open overlay, if any.</summary>
		public static void Hide()
		{
			if (Current != null)
			{
				Current.Close();
			}
		}

		internal static void NotifyClosed(SettingsOverlayView view)
		{
			if (Current == view)
			{
				Current = null;
			}

			Closed?.Invoke();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			Current = null;
			Closed = null;
			ReplayTutorialHook = null;
		}

		private static void EnsureEventSystem()
		{
			if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null)
			{
				return;
			}

			GameObject eventSystem = new GameObject(nameof(EventSystem), typeof(EventSystem), typeof(InputSystemUIInputModule));
			Debug.Log("[SettingsOverlay] No EventSystem in the scene; added one.", eventSystem);
		}
	}
}
