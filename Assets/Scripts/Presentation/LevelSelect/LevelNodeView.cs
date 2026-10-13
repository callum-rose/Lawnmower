using System;
using System.Collections;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LevelSelect
{
	/// <summary>One numbered stop on the Level Select garden path. Passive: draws a <see cref="LevelNodeState"/>, reports taps.</summary>
	[RequireComponent(typeof(Button))]
	public class LevelNodeView : MonoBehaviour
	{
		[SerializeField] private UiTheme theme;
		[SerializeField] private RectTransform body;
		[SerializeField] private Image face;
		[SerializeField] private Image shadow;
		[SerializeField] private TMP_Text number;
		[SerializeField] private Image lockIcon;
		[Tooltip("The yellow tick badge: only on perfect levels.")]
		[SerializeField] private GameObject perfectBadge;
		[SerializeField] private GameObject nextTag;
		[SerializeField] private GameObject bunny;

		[Header("Look")]
		[SerializeField] private float size = 64f;
		[SerializeField] private float nextSize = 80f;
		[SerializeField] private float numberSize = 32f;
		[SerializeField] private float nextNumberSize = 40f;
		[Tooltip("Figma's locked-node / tab-track orange (#F3B73A); not a theme token.")]
		[SerializeField] private Color lockedFace = new Color(0.953f, 0.718f, 0.227f);

		private Button _button;
		private Coroutine _shake;

		public event Action Clicked;

		public LevelNodeState State { get; private set; }
		public int Number { get; private set; }
		public Button Button => _button != null ? _button : _button = GetComponent<Button>();

		private void Awake() => Button.onClick.AddListener(() => Clicked?.Invoke());

		public void Show(LevelNodeState state, int levelNumber)
		{
			State = state;
			Number = levelNumber;

			bool isNext = state == LevelNodeState.Next;
			bool isDone = state == LevelNodeState.Completed || state == LevelNodeState.Perfect;
			bool isLocked = state == LevelNodeState.Locked;

			float side = isNext ? nextSize : size;
			body.sizeDelta = new Vector2(side, side);
			((RectTransform)transform).sizeDelta = new Vector2(side, side);

			face.color = isLocked ? lockedFace : isDone ? theme.Get(ThemeColor.Green) : theme.Get(ThemeColor.White);
			shadow.enabled = !isLocked;
			shadow.color = isDone ? theme.Get(ThemeColor.GreenShadow) : theme.Get(ThemeColor.YellowShadow);

			number.gameObject.SetActive(!isLocked);
			number.text = levelNumber.ToString();
			number.fontSize = isNext ? nextNumberSize : numberSize;
			number.color = isDone ? theme.Get(ThemeColor.White) : isNext ? theme.Get(ThemeColor.Red) : theme.Get(ThemeColor.Ink);

			lockIcon.gameObject.SetActive(isLocked);
			perfectBadge.SetActive(state == LevelNodeState.Perfect);
			nextTag.SetActive(isNext);
			bunny.SetActive(isNext);
		}

		/// <summary>A quick side-to-side wobble: "can't play this one yet".</summary>
		public void Shake()
		{
			if (!isActiveAndEnabled)
			{
				return;
			}

			if (_shake != null)
			{
				StopCoroutine(_shake);
			}

			_shake = StartCoroutine(ShakeRoutine());
		}

		private IEnumerator ShakeRoutine()
		{
			const float duration = 0.35f;
			for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
			{
				float damp = 1f - t / duration;
				body.anchoredPosition = new Vector2(Mathf.Sin(t * 60f) * 6f * damp, 0f);
				yield return null;
			}

			body.anchoredPosition = Vector2.zero;
			_shake = null;
		}
	}
}
