using System;
using Lean.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Lean.Touch
{
	/// <summary>
	///     This component fires events on the first frame where a finger has been touching the screen for more than
	///     <b>TapThreshold</b> seconds, and is therefore no longer eligible for tap or swipe events.
	/// </summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanFingerOld")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Finger Old")]
	public class LeanFingerOld : MonoBehaviour
	{
		/// <summary>Ignore fingers with StartedOverGui?</summary>
		public bool IgnoreStartedOverGui = true;

		/// <summary>Ignore fingers with OverGui?</summary>
		public bool IgnoreIsOverGui;

		/// <summary>Do nothing if this LeanSelectable isn't selected?</summary>
		public LeanSelectable RequiredSelectable;

		[SerializeField] private LeanFingerEvent onFinger;

		/// <summary>
		///     The method used to find world coordinates from a finger. See LeanScreenDepth documentation for more
		///     information.
		/// </summary>
		public LeanScreenDepth ScreenDepth = new LeanScreenDepth(LeanScreenDepth.ConversionType.DepthIntercept);

		[SerializeField] private Vector3Event onPosition;

		/// <summary>Called on the first frame the conditions are met.</summary>
		public LeanFingerEvent OnFinger
		{
			get
			{
				if (onFinger == null) onFinger = new LeanFingerEvent();
				return onFinger;
			}
		}

		/// <summary>
		///     Called on the first frame the conditions are met.
		///     Vector3 = Start point based on the ScreenDepth settings.
		/// </summary>
		public Vector3Event OnPosition
		{
			get
			{
				if (onPosition == null) onPosition = new Vector3Event();
				return onPosition;
			}
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			RequiredSelectable = GetComponentInParent<LeanSelectable>();
		}
#endif

		protected virtual void Start()
		{
			if (RequiredSelectable == null) RequiredSelectable = GetComponentInParent<LeanSelectable>();
		}

		protected virtual void OnEnable()
		{
			LeanTouch.OnFingerOld += HandleFingerOld;
		}

		protected virtual void OnDisable()
		{
			LeanTouch.OnFingerOld -= HandleFingerOld;
		}

		private void HandleFingerOld(LeanFinger finger)
		{
			// Ignore?
			if (IgnoreStartedOverGui && finger.StartedOverGui) return;

			if (IgnoreIsOverGui && finger.IsOverGui) return;

			if (RequiredSelectable != null && RequiredSelectable.IsSelected == false) return;

			if (onFinger != null) onFinger.Invoke(finger);

			if (onPosition != null)
			{
				var position = ScreenDepth.Convert(finger.ScreenPosition, gameObject);

				onPosition.Invoke(position);
			}
		}

		[Serializable]
		public class LeanFingerEvent : UnityEvent<LeanFinger>
		{
		}

		[Serializable]
		public class Vector3Event : UnityEvent<Vector3>
		{
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Inspector
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(LeanFingerOld))]
	public class LeanFingerOld_Inspector : LeanInspector<LeanFingerOld>
	{
		private bool showUnusedEvents;

		protected override void DrawInspector()
		{
			Draw("IgnoreStartedOverGui", "Ignore fingers with StartedOverGui?");
			Draw("IgnoreIsOverGui", "Ignore fingers with OverGui?");
			Draw("RequiredSelectable", "Do nothing if this LeanSelectable isn't selected?");

			EditorGUILayout.Separator();

			var usedA = Any(t => t.OnFinger.GetPersistentEventCount() > 0);
			var usedB = Any(t => t.OnPosition.GetPersistentEventCount() > 0);

			EditorGUI.BeginDisabledGroup(usedA && usedB);
			showUnusedEvents = EditorGUILayout.Foldout(showUnusedEvents, "Show Unused Events");
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Separator();

			if (usedA || showUnusedEvents) Draw("onFinger");

			if (usedB || showUnusedEvents)
			{
				Draw("ScreenDepth");
				Draw("onPosition");
			}
		}
	}
}
#endif